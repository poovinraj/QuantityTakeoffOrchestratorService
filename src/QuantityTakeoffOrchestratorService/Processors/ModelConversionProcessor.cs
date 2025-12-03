using Flurl.Util;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using NewRelic.Api.Agent;
using QuantityTakeoffOrchestratorService.Enums;
using QuantityTakeoffOrchestratorService.Helpers;
using QuantityTakeoffOrchestratorService.Models.Constants;
using QuantityTakeoffOrchestratorService.Models.Domain;
using QuantityTakeoffOrchestratorService.Models.Mapping;
using QuantityTakeoffOrchestratorService.Models.Request;
using QuantityTakeoffOrchestratorService.Models.View;
using QuantityTakeoffOrchestratorService.NotificationHubs;
using QuantityTakeoffOrchestratorService.Processors.Interfaces;
using QuantityTakeoffOrchestratorService.Services;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Trimble.Technology.TrimBim;

namespace QuantityTakeoffOrchestratorService.Processors;

/// <summary>
///     ModelConversionProcessor is responsible for processing model conversion requests.
/// </summary>
public class ModelConversionProcessor : IModelConversionProcessor
{
    private readonly IConnectClientService _connectClient;
    private readonly ITrimbleFileService _trimbleFileService;
    private readonly ILogger<ModelConversionProcessor> _logger;
    private readonly IModelMetaDataProcessor _modelMetaDataProcessor;

    /// <summary>
    ///  itializes a new instance of the <see cref="ModelConversionProcessor"/> class.
    /// </summary>
    /// <param name="connectClientService"></param>
    /// <param name="trimbleFileService"></param>
    /// <param name="logger"></param>
    public ModelConversionProcessor(
        IConnectClientService connectClientService,
        ITrimbleFileService trimbleFileService,
        ILogger<ModelConversionProcessor> logger,
        IHubContext<QuantityTakeoffOrchestratorHub> hubContext,
        IModelMetaDataProcessor modelMetaDataProcessor)
    {
        this._connectClient = connectClientService;
        this._trimbleFileService = trimbleFileService;
        _logger = logger;
        _modelMetaDataProcessor = modelMetaDataProcessor;
    }

    /// <summary>
    /// Processes a BIM model from Trimble Connect, converting it to a format optimized for quantity takeoff
    /// operations and storing the processed data in Trimble File Service.
    /// </summary>
    /// <param name="conversionRequest">The model conversion request containing all necessary parameters</param>
    /// <returns>A result object with details about the processed model or error information</returns>
    [Trace]
    public async Task<ModelProcessingResult> ConvertTrimBimModelAndUploadToFileService(ModelConversionRequest conversionRequest)
    {
        var stopwatch = Stopwatch.StartNew();
        GC.Collect(); // Force garbage collection

        var memoryUsageStart = GetMemoryUsage();        
        try
        {
            // Log model details to New Relic for tracing
            NewRelicHelper.AddCustomLoggingAttributes(new Dictionary<string, string?>
            {
                { "JobModelId", conversionRequest.JobModelId },
                { "connectModelId", conversionRequest.TrimbleConnectModelId },
            });

            _logger.LogInformation("ConvertTrimBimModelAndUploadToFileService: 1 - Starting model conversion with initial memory usage: {MemoryUsageMB}MB for ModelReferenceId: {ModelReferenceId}",
                memoryUsageStart, conversionRequest.TrimbleConnectModelId);

            // Step 1: Download and parse the BIM model
            _logger.LogInformation("ConvertTrimBimModelAndUploadToFileService: 2 - Starting model download and parsing for ModelReferenceId: {ModelReferenceId}",
                conversionRequest.TrimbleConnectModelId);
            var model = await ProcessTrimBim(
                conversionRequest.UserAccessToken,
                conversionRequest.TrimbleConnectModelId,
                conversionRequest.ModelVersionId);

            var memoryAfterModelLoad = GetMemoryUsage();
            _logger.LogInformation("ConvertTrimBimModelAndUploadToFileService: 3 - Memory after model load: {MemoryUsageMB}MB (Δ {MemoryDeltaMB}MB) for ModelReferenceId: {ModelReferenceId}",
                memoryAfterModelLoad, memoryAfterModelLoad - memoryUsageStart, conversionRequest.TrimbleConnectModelId);

            if (model is null)
            {
                _logger.LogError("Model is null after processing for ModelReferenceId: {ModelReferenceId}",
                    conversionRequest.TrimbleConnectModelId);
                return CreateFailureResult(conversionRequest, "Model is null after processing.");
            }

            // Step 2: Extract elements and create JSON
            _logger.LogInformation("Generating takeoff elements JSON for ModelReferenceId: {ModelReferenceId}",
                conversionRequest.TrimbleConnectModelId);
            
            // Use the stream-based version to prevent memory issues
            var tempJsonFilePath = await Generate3DTakeoffElementsJsonAsync(
                conversionRequest.TrimbleConnectModelId,
                model);

            var memoryAfterJsonGeneration = GetMemoryUsage();
            _logger.LogInformation("ConvertTrimBimModelAndUploadToFileService: 4 - Memory after JSON generation: {MemoryUsageMB}MB (Δ {MemoryDeltaMB}MB), for ModelReferenceId: {ModelReferenceId}",
                memoryAfterJsonGeneration, memoryAfterJsonGeneration - memoryAfterModelLoad,  conversionRequest.TrimbleConnectModelId);

            // Step 3: Upload processed model to file service
            string fileId;
            try
            {
                // Get the file size
                var fileInfo = new FileInfo(tempJsonFilePath);
                long jsonByteSize = fileInfo.Length;
                
                _logger.LogInformation("Uploading JSON to file service for ModelReferenceId: {ModelReferenceId}, File size: {JsonSizeMB:F2}MB",
                        conversionRequest.TrimbleConnectModelId, jsonByteSize / (1024.0 * 1024.0));

                // Read the file back into memory in a controlled manner for upload
                using (var jsonStream = fileInfo.OpenRead())
                {
                    fileId = await UploadToConnectFileService(
                        conversionRequest.SpaceId,
                        conversionRequest.FolderId,
                        conversionRequest.TrimbleConnectModelId,
                        jsonStream);
                    jsonStream.Close();
                }
                fileInfo = null;
            }
            finally
            {
                if(!string.IsNullOrEmpty(tempJsonFilePath) && File.Exists(tempJsonFilePath))
                {
                    try
                    {
                        File.Delete(tempJsonFilePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete temporary JSON file: {FilePath}", tempJsonFilePath);
                        throw;
                    }
                }
            }

            GC.Collect(); // Request garbage collection

            var memoryAfterUpload = GetMemoryUsage();
            _logger.LogInformation("ConvertTrimBimModelAndUploadToFileService: 5 -  Memory after upload: {MemoryUsageMB}MB (Δ {MemoryDeltaMB}MB) for ModelReferenceId: {ModelReferenceId}",
                memoryAfterUpload, memoryAfterUpload - memoryAfterJsonGeneration, conversionRequest.TrimbleConnectModelId);

            // These two operations can run in parallel
            var uniquePropertiesTask = Task.Run(() => ProcessModelAndFetchUniquePropertyDefinitions(model));
            var fileDownloadUrlTask = GetFileDownloadUrlFromFileService(conversionRequest.SpaceId, fileId);

            // Wait for both tasks to complete
            await Task.WhenAll(uniquePropertiesTask, fileDownloadUrlTask);

            var uniquePropertyDefinitions = uniquePropertiesTask.Result;
            var fileDownloadUrl = await fileDownloadUrlTask;

            model = null; // Release model from memory
            GC.Collect(); // Request garbage collection
            GC.WaitForFullGCComplete();

            var memoryAfterModelRelease = GetMemoryUsage();
            _logger.LogInformation("ConvertTrimBimModelAndUploadToFileService: 6 - Memory after model release: {MemoryUsageMB}MB (Δ {MemoryDeltaMB}MB) for ModelReferenceId: {ModelReferenceId}",
                memoryAfterModelRelease, memoryAfterModelRelease - memoryAfterUpload, conversionRequest.TrimbleConnectModelId);

            // Step 4: Update model metadata in the database
            await _modelMetaDataProcessor.UpdateFileIdAndPSetDefinitionsForConnectModel(
                conversionRequest.TrimbleConnectModelId,
                fileId,
                uniquePropertyDefinitions,
                conversionRequest.CustomerId);

            uniquePropertyDefinitions = null; // Release from memory
            var finalMemory = GetMemoryUsage();
            stopwatch.Stop();

            // Return success result
            return new ModelProcessingResult
            {
                JobModelId = conversionRequest.JobModelId,
                TrimbleConnectModelId = conversionRequest.TrimbleConnectModelId,
                IsConversionSuccessful = true,
                ModelFileDownloadUrl = fileDownloadUrl
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var memoryAtException = GetMemoryUsage();
            var processInfo = GetProcessMemoryInfo();
            
            string errorMessage = $"Failed to process model conversion request for ModelReferenceId: {conversionRequest.TrimbleConnectModelId}";
            _logger.LogError(ex, "{ErrorMessage}. Memory at exception: {MemoryUsageMB}MB (Δ {MemoryDeltaMB}MB), " +
                "Working Set: {WorkingSet}MB, Private Bytes: {PrivateBytes}MB, Process time: {ElapsedMs}ms", 
                errorMessage, memoryAtException, memoryAtException - memoryUsageStart, 
                processInfo.workingSetMB, processInfo.privateBytesMB, stopwatch.ElapsedMilliseconds);

            GC.Collect(); // Try to recover memory

            return new ModelProcessingResult
            {
                JobModelId = conversionRequest.JobModelId,
                TrimbleConnectModelId = conversionRequest.TrimbleConnectModelId,
                IsConversionSuccessful = false,
                ErrorMessage = $"{errorMessage}: {ex.Message}"
            };
        }
    }

    #region Helper methods

    /// <summary>
    /// Gets memory usage in MB for the current process
    /// </summary>
    private static long GetMemoryUsage()
    {
        return GC.GetTotalMemory(false) / (1024 * 1024);
    }

    /// <summary>
    /// Gets detailed process memory information
    /// </summary>
    private static (long workingSetMB, long privateBytesMB) GetProcessMemoryInfo()
    {
        try
        {
            using var process = Process.GetCurrentProcess();
            return (
                workingSetMB: process.WorkingSet64 / (1024 * 1024),
                privateBytesMB: process.PrivateMemorySize64 / (1024 * 1024)
            );
        }
        catch
        {
            return (-1, -1); // Return -1 if we can't get process info
        }
    }

    /// <summary>
    /// Creates a failure result with the specified error message
    /// </summary>
    private static ModelProcessingResult CreateFailureResult(ModelConversionRequest request, string errorMessage)
    {
        return new ModelProcessingResult
        {
            TrimbleConnectModelId = request.TrimbleConnectModelId,
            IsConversionSuccessful = false,
            ErrorMessage = errorMessage
        };
    }

    private async Task<IModel?> ProcessTrimBim(string userAccessToken, string trimbleConnectModelId, string? modelFileVersionId = null)
    {
        IModel? result = null;
        byte[]? modelBlob = await _connectClient.DownloadModelFile(userAccessToken, trimbleConnectModelId, modelFileVersionId).ConfigureAwait(false);
        if (!Trimble.Technology.TrimBim.Model.TryParse(modelBlob, out result))
        {
            string errorMessage = "Failed to parse the model.";
            throw new Exception(errorMessage);
        }
        return result;
    }

    [Trace]
    private async Task< string> Generate3DTakeoffElementsJsonAsync(string trimbleConnectModelId, IModel model)
    {
        if (model is null)
        {
            return (string.Empty);
        }

        try
        {
            var memoryBeforeProcessing = GetMemoryUsage();
            _logger.LogInformation("Generate3DTakeoffElementsJson : Starting property processing for ModelReferenceId: {ModelReferenceId}, Memory: {MemoryMB}MB", 
                trimbleConnectModelId, memoryBeforeProcessing);

            var propertyMappings = ProcessModelProperties(model);
            
            var memoryAfterPropertyMapping = GetMemoryUsage();
            _logger.LogInformation("Generate3DTakeoffElementsJson: Property mappings completed for ModelReferenceId: {ModelReferenceId}, Memory: {MemoryMB}MB (Δ {MemoryDeltaMB}MB)", 
                trimbleConnectModelId, memoryAfterPropertyMapping, memoryAfterPropertyMapping - memoryBeforeProcessing);

            var quantityTakeoffElements = FetchBasicTakeOffElements(model, trimbleConnectModelId).Select(element =>
            {
                if (propertyMappings.TryGetValue(element.Model3DItemIdIndex.Idx, out var properties))
                {
                    element.Properties = properties;
                }
                return element;
            }).ToList();

            var memoryAfterElementFetch = GetMemoryUsage();
            _logger.LogInformation("Generate3DTakeoffElementsJson: Total takeoff elements processed: {Count} for ModelReferenceId: {ModelReferenceId}, Memory: {MemoryMB}MB (Δ {MemoryDeltaMB}MB)",
                quantityTakeoffElements.Count, trimbleConnectModelId, memoryAfterElementFetch, memoryAfterElementFetch - memoryAfterPropertyMapping);

            // Create a temporary file to store the JSON data
            var tempFilePath = Path.GetTempFileName();
            
            try
            {
                _logger.LogInformation("Generate3DTakeoffElementsJson: Starting JSON serialization to temp file for ModelReferenceId: {ModelReferenceId}", 
                    trimbleConnectModelId);
                
                // Using a FileStream for the JSON output to avoid memory issues
                using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 
                    bufferSize: 32768, useAsync: true))
                {
                    await GenerateJsonToStreamAsync(quantityTakeoffElements, fileStream);
                }
                
                quantityTakeoffElements = null; // Release memory
                propertyMappings = null; // Release memory
                GC.Collect(); // Force garbage collection
                
                var memoryAfterSerialization = GetMemoryUsage();
                _logger.LogInformation("Generate3DTakeoffElementsJson: JSON serialization completed for ModelReferenceId: {ModelReferenceId}, Memory: {MemoryMB}MB (Δ {MemoryDeltaMB}MB)", 
                    trimbleConnectModelId, memoryAfterSerialization, memoryAfterSerialization - memoryAfterElementFetch);
                
                return  tempFilePath;
            }
            catch
            {
                // Clean up the temp file
                try
                {
                    if (File.Exists(tempFilePath))
                    {
                        File.Delete(tempFilePath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Generate3DTakeoffElementsJson: Failed to delete temporary JSON file: {FilePath}", tempFilePath);
                }

                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating JSON for ModelReferenceId: {ModelReferenceId}", trimbleConnectModelId);
            throw;
        }
    }

    private async Task GenerateJsonToStreamAsync(List<QuantityTakeoffElement> elements, Stream outputStream)
    {
        const int batchSize = 1000; // Process in smaller batches to manage memory

        using var writer = new Utf8JsonWriter(outputStream, new JsonWriterOptions
        {
            Indented = false,
            SkipValidation = true
        });
        
        // Start JSON array
        writer.WriteStartArray();
        
        // Process elements in batches
        for (int i = 0; i < elements.Count; i += batchSize)
        {
            var batch = elements.Skip(i).Take(batchSize).ToList();
            
            foreach (var element in batch)
            {
                // Create a dictionary to hold model properties
                var modelProperties = element.Properties?
                    .GroupBy(x => x.PropKey)
                    .Select(g => g.First()) // Take the first distinct entry
                    .ToDictionary(x => x.PropKey, x => x.PropValue) ?? new Dictionary<string, string>();

                // Deserialize the element to an ExpandoObject
                var expandedResult = BsonSerializer.Deserialize<ExpandoObject>(element.ToBsonDocument());
                
                // Add model properties to the ExpandoObject
                foreach (var kvp in modelProperties)
                {
                    ((IDictionary<string, object>)expandedResult).TryAdd(kvp.Key, kvp.Value);
                }
                
                // Add the ID field
                ((IDictionary<string, object>)expandedResult).TryAdd(FieldNames.id.ToString(), element.Id);
                
                // Remove the "properties" field if it exists
                _ = ((IDictionary<string, object>)expandedResult).Remove("properties");
                
                // Serialize this item to the JSON writer
                JsonSerializer.Serialize(writer, expandedResult);
            }
            
            // Flush the writer after each batch
            await writer.FlushAsync();
            
            // Clear the batch to help with memory
            batch.Clear();
        }
        
        // End JSON array
        writer.WriteEndArray();
        await writer.FlushAsync();
    }

    private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
    {
        WriteIndented = false // Remove indentation for better performance
    };

    [Trace]
    private async Task<string> UploadToConnectFileService(string spaceId, string folderId, string trimbleConnectModelId, Stream dataStream)
    {
        try
        {
            _logger.LogInformation("Starting file upload to service for ModelReferenceId: {ModelReferenceId}, content size: {ContentSizeMB:F2}MB", 
                trimbleConnectModelId, dataStream.Length / (1024.0 * 1024.0));
            
            string fileId = await _trimbleFileService.UploadFileAsync(
                spaceId,
                folderId,
                trimbleConnectModelId + ".json",
                dataStream
            );

            _logger.LogInformation("Completed file upload for ModelReferenceId: {ModelReferenceId}, FileId: {FileId}", 
                trimbleConnectModelId, fileId);
                
            return fileId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading takeoff elements to Trimble File Service for ModelReferenceId: {ModelReferenceId}", 
                trimbleConnectModelId);
            throw new Exception($"Failed to upload takeoff elements to Trimble File Service: {ex.Message}", ex);
        }
    }

    private Dictionary<int, List<ModelProperties>> ProcessModelProperties(IModel model)
    {
        var memoryBefore = GetMemoryUsage();
        _logger.LogDebug("Starting ProcessModelProperties. Memory: {MemoryMB}MB", memoryBefore);
        
        var result = new ConcurrentDictionary<int, List<ModelProperties>>();
        var lockObject = new object(); // Lock object for synchronization

        // Run the methods in parallel
        var productPsetPropertiesTask = Task.Run(() => GetProductPsetProperties(model));
        var referenceObjectPropertiesTask = Task.Run(() => GetReferenceObjectProperties(model));
        var layerPropertiesTask = Task.Run(() => GetLayerProperties(model));
        var otherPropertiesTask = Task.Run(() => GetOtherProperties(model));

        // Wait for all tasks to complete
        Task.WaitAll(productPsetPropertiesTask, referenceObjectPropertiesTask, layerPropertiesTask, otherPropertiesTask);

        // Retrieve the results
        var productPsetProperties = productPsetPropertiesTask.Result;
        var referenceObjectProperties = referenceObjectPropertiesTask.Result;
        var layerProperties = layerPropertiesTask.Result;
        var otherProperties = otherPropertiesTask.Result;

        var memoryAfterProperties = GetMemoryUsage();
        _logger.LogDebug("Property dictionaries created. Memory: {MemoryMB}MB (Δ {MemoryDeltaMB}MB)", 
            memoryAfterProperties, memoryAfterProperties - memoryBefore);

        // Aggregate properties by entity ID
        otherProperties.AsParallel().ForAll(binding =>
        {
            var (entityIds, properties) = binding;
            foreach (var entityId in entityIds)
            {
                var entityIdx = (int)entityId;
                var allProps = new List<ModelProperties>();
                allProps.AddRange(properties);
                if (referenceObjectProperties.TryGetValue(entityIdx, out var refProps) && refProps.Count > 0)
                {
                    allProps.AddRange(refProps);
                }

                if (productPsetProperties.TryGetValue(entityIdx, out var productProps) && productProps.Count > 0)
                {
                    allProps.AddRange(productProps);
                }

                if (layerProperties.TryGetValue(entityIdx, out var layerProps) && layerProps.Count > 0)
                {
                    allProps.AddRange(layerProps);
                }
                if (allProps.Count > 0)
                {
                    lock (lockObject) // Ensure exclusive access to the dictionary
                    {
                        result.AddOrUpdate(entityIdx, allProps, (key, existingList) =>
                        {
                            existingList.AddRange(allProps);
                            return existingList;
                        });
                    }
                }
            }
        });

        var memoryAfterMerge = GetMemoryUsage();
        _logger.LogDebug("Property merge completed. Memory: {MemoryMB}MB (Δ {MemoryDeltaMB}MB)", 
            memoryAfterMerge, memoryAfterMerge - memoryAfterProperties);

        return result
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    private Dictionary<int, List<ModelProperties>> GetLayerProperties(IModel model)
    {
        var result = new Dictionary<int, List<ModelProperties>>(model.Entities.EntityInformations.Count);
        var layerProperty = PresentationLayerPset.Property;

        // Group instances by their EntityId
        var groupedInstances = model.Geometry.Instances
            .GroupBy(instance => instance.EntityId);

        foreach (var group in groupedInstances)
        {
            var entityIdx = (int)group.Key;

            // Get distinct layer names for the current entity
            var layerNames = group
                .Select(instance => (int)instance.LayerId)
                .Distinct()
                .Select(layerId => model.Geometry.Layers[layerId])
                .OrderBy(layerName => layerName);

            var combinedLayerNames = string.Join(", ", layerNames);

            // Create model properties for the current entity
            var modelProperties = new List<ModelProperties>
        {
            new Models.View.ModelProperties
            {
                PropKey = layerProperty,
                PropValue = combinedLayerNames,
                PropValueType = PropertyType.StringValue
            }
        };

            // Add the entity ID and its properties to the result dictionary
            result[entityIdx] = modelProperties;
        }

        return result;
    }

    private Dictionary<int, List<ModelProperties>> GetProductPsetProperties(IModel model)
    {
        var result = new Dictionary<int, List<ModelProperties>>(model.Entities.EntityInformations.Count);
        var productInformation = model.Properties.ProductInformation;

        // Create a mapping of entity IDs to their product details
        var entityProductMap = productInformation.Bindings
            .GroupBy(binding => binding.EntityId)
            .ToDictionary(
                group => (int)group.Key,
                group =>
                {
                    var binding = group.First(); // Take first binding if multiple exist
                    var product = productInformation.Products[(int)binding.ProductInformationId];
                    return (NameId: (int)product.NameId, DescriptionId: (int)product.DescriptionId, ObjectTypeId: (int)product.ObjectTypeId, OwnerId: (int)product.OwnerId, HistoryId: (int)product.HistoryId);
                }
            );

        // Generate properties for all entities at once
        foreach (var (entityId, productInfo) in entityProductMap)
        {
            var properties = ProductPset.Properties
                // .Where(x => modelProperties?.Contains(x) ?? false)
                .Select(x => new ModelProperties
                {
                    PropKey = x,
                    PropValueType = PropertyType.StringValue,
                    PropValue = x.Replace($",{ProductPset.PSetName}", string.Empty) switch
                    {
                        ProductPset.PropertyNames.ProductName => productInformation.Names[productInfo.NameId],
                        ProductPset.PropertyNames.ProductDescription => productInformation.Descriptions[productInfo.DescriptionId],
                        ProductPset.PropertyNames.ProductObjectType => productInformation.ObjectTypes[productInfo.ObjectTypeId],
                        ProductPset.PropertyNames.OwningUser => productInformation.Owners[productInfo.OwnerId].PersonId,
                        ProductPset.PropertyNames.Application => GetProductApplicationText(productInformation.Owners[productInfo.OwnerId]),
                        ProductPset.PropertyNames.CreationDate => productInformation.History[productInfo.HistoryId].CreationDate.ToString(),
                        ProductPset.PropertyNames.LastModifiedDate => productInformation.History[productInfo.HistoryId].LastModificationDate.ToString(),
                        ProductPset.PropertyNames.ChangeAction => productInformation.History[productInfo.HistoryId].ChangeAction.ToString(),
                        ProductPset.PropertyNames.State => productInformation.History[productInfo.HistoryId].HistoryState.ToString(),
                        _ => throw new Exception()
                    }
                    // PropValue = productInformation.Names[productInfo.NameId]
                })
                .ToList();

            if (properties.Count != 0)
            {
                result.Add(entityId, properties);
            }
        }

        return result;
    }

    private string GetProductApplicationText(Owner productOwner)
    {
        return $"{productOwner.ApplicationFullName ?? string.Empty} ({productOwner.ApplicationIdentifier ?? string.Empty} v{productOwner.ApplicationVersion ?? string.Empty})";
    }

    private ParallelQuery<(IList<uint>, List<ModelProperties>)> GetOtherProperties(IModel model)
    {
        var propertySets = model.Properties.PropertySets;

        // Process property set bindings in parallel
        var otherProperties = Enumerable.Range(0, propertySets.PropertySetBindings.Count)
            .AsParallel()
            .Select(bindingIdx =>
            {
                var binding = propertySets.PropertySetBindings[bindingIdx];
                var psetDef = propertySets.PropertySetDefinitions[(int)binding.PropertySetDefinitionId];
                var psetName = propertySets.PropertySetNames[(int)psetDef.NameId];

                var properties = psetDef.Properties
                    .Select((propertyDefinition, index) =>
                    {
                        var propertyName = propertySets.PropertyNames[(int)propertyDefinition.PropertyNameId];
                        var key = $"{propertyName},{psetName}";
                        var value = GetValue(model, propertyDefinition, (int)binding.Values[index]);

                        return new ModelProperties
                        {
                            PropKey = key,
                            PropValue = value,
                            PropValueType = propertyDefinition.Type
                        };
                    })
                    .Where(p => p != null) // Filter out nulls
                    .ToList();

                return (binding.EntityIds, properties);
            });
        return otherProperties;
    }

    /// <summary>
    /// returns Property Values
    /// </summary>
    /// <param name="propertyDefinition"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    private string GetValue(IModel model, SinglePropertyDefinition propertyDefinition, int value)
    {
        return propertyDefinition.Type switch
        {
            PropertyType.LengthMeasure => model.Properties.PropertySets.LengthMeasures[value].ToString(CultureInfo.InvariantCulture),
            PropertyType.AreaMeasure => model.Properties.PropertySets.AreaMeasures[value].ToString(CultureInfo.InvariantCulture),
            PropertyType.VolumeMeasure => model.Properties.PropertySets.VolumeMeasures[value].ToString(CultureInfo.InvariantCulture),
            PropertyType.MassMeasure => model.Properties.PropertySets.MassMeasures[value].ToString(CultureInfo.InvariantCulture),
            PropertyType.AngleMeasure => model.Properties.PropertySets.AngleMeasures[value].Degrees.ToString(),
            PropertyType.StringValue => model.Properties.PropertySets.StringValues[value],
            PropertyType.IntValue => value.ToString(CultureInfo.InvariantCulture),
            PropertyType.DoubleValue => model.Properties.PropertySets.DoubleValues[value].ToString(CultureInfo.InvariantCulture),
            PropertyType.DateTime => model.Properties.PropertySets.DateTimeValues[value].ToString("O", CultureInfo.InvariantCulture),
            PropertyType.Logical => value == 0 ? "false" : (value == 1 ? "true" : null),
            PropertyType.Boolean => value == 0 ? "false" : "true",
            _ => "?",
        };
    }

    // Process the entire model and return a dictionary containing entity IDs and their model properties
    private Dictionary<int, List<ModelProperties>> GetReferenceObjectProperties(IModel model)
    {
        var result = new Dictionary<int, List<ModelProperties>>(model.Entities.EntityInformations.Count);

        // Iterate over all entities in the model
        foreach (var entityIdx in Enumerable.Range(0, model.Entities.EntityInformations.Count))
        {
            var entityInfo = model.Entities.EntityInformations[entityIdx];
            var fileFormatAndCommonTypeData = GetFileFormatAndCommonType(entityInfo?.ClassName);

            // Generate model properties for the current entity
            var modelProperties = ReferenceObjectPset.Properties
                .Select(x => new ModelProperties
                {
                    PropKey = x,
                    PropValue = x.Replace($",{ReferenceObjectPset.PSetName}", string.Empty) switch
                    {
                        ReferenceObjectPset.PropertyNames.FileFormat => fileFormatAndCommonTypeData.Item1,
                        ReferenceObjectPset.PropertyNames.CommonType => fileFormatAndCommonTypeData.Item2,
                        ReferenceObjectPset.PropertyNames.GuidIFC => entityInfo?.Identifier.ToString() ?? "",
                        ReferenceObjectPset.PropertyNames.GuidMS => (entityInfo?.Identifier as GuidIdentifier)?.Guid.ToString() ?? "",
                        _ => throw new Exception($"Unknown property key: {x}")
                    },
                    PropValueType = PropertyType.StringValue,
                })
                .ToList();

            // Add the entity ID and its properties to the result dictionary
            result[entityIdx] = modelProperties;
        }

        return result;
    }

    private (string, string) GetFileFormatAndCommonType(string? className)
    {
        if (string.IsNullOrWhiteSpace(className)) return (string.Empty, string.Empty);

        if (className.StartsWith("IFC", StringComparison.OrdinalIgnoreCase) ||
            className.StartsWith("DGN", StringComparison.OrdinalIgnoreCase)) return (className.Substring(0, 3), className.Substring(3));
        if (className.StartsWith("AC", StringComparison.OrdinalIgnoreCase)) return ("DWG", className.Substring(2));
        if (className.StartsWith("AEC", StringComparison.OrdinalIgnoreCase)) return ("DWG", className.Substring(3));

        return ("Invalid", "Invalid");
    }

    private List<ExpandoObject> CreateExpandoObjects(IEnumerable<QuantityTakeoffElement> quantityTakeoffElements)
    {
        var quantityTakeoffItems = new List<ExpandoObject>(quantityTakeoffElements.Count()); // Preallocate list capacity

        _ = Parallel.ForEach(quantityTakeoffElements, element =>
        {
            // Use a dictionary to store model properties efficiently
            var modelProperties = element.Properties?
                .GroupBy(x => x.PropKey)
                .Select(g => g.First()) // Take the first distinct entry
                .ToDictionary(x => x.PropKey, x => x.PropValue) ?? new Dictionary<string, string>();

            // Deserialize the element to an ExpandoObject
            var expandedResult = BsonSerializer.Deserialize<ExpandoObject>(element.ToBsonDocument());

            // Add model properties to the ExpandoObject
            foreach (var kvp in modelProperties)
            {
                ((IDictionary<string, object>)expandedResult).TryAdd(kvp.Key, kvp.Value);
            }

            // Add the ID field
            ((IDictionary<string, object>)expandedResult).TryAdd(FieldNames.id.ToString(), element.Id);

            // Remove the "properties" field if it exists
            _ = ((IDictionary<string, object>)expandedResult).Remove("properties");

            // Add the result to the list
            quantityTakeoffItems.Add(expandedResult);
        });

        return quantityTakeoffItems;
    }

    private IEnumerable<QuantityTakeoffElement> FetchBasicTakeOffElements(IModel model, string trimbleConnectModelId)
    {
        return model.Geometry.Instances.Select(x => x.EntityId).Distinct()
            .Select(entityIdx =>
            {
                var entity = model.Entities.EntityInformations[(int)entityIdx];
                return new QuantityTakeoffElement
                {
                    Id = entityIdx.ToString(),
                    ReferenceId = trimbleConnectModelId,
                    ItemType = GetItemType(entity.ClassName),
                    IfcClassName = entity.ClassName,
                    Origin = OriginType.Model3D,
                    Count = new QuantityValueView
                    {
                        Value = 1
                    },
                    Model3DItemIdIndex = new Model3DItemIdIndex
                    {
                        Id = entity.Identifier.ToString(),
                        Idx = (int)entityIdx
                    }
                };
            }).ToList();
    }

    /// <summary>
    /// returns valid itemtype based on input types
    /// </summary>
    /// <param name="className"></param>
    /// <returns></returns>
    private ItemType GetItemType(string className)
    {
        return Model3DItemTypesMapper.modelItemTypesMapping.FirstOrDefault(types => string.Equals(types.Key, className, StringComparison.OrdinalIgnoreCase), new KeyValuePair<string, ItemType>("other", ItemType.other)).Value;
    }

    /// <summary>
    /// Gets the unique set of properties in a model
    /// </summary>
    /// <param name="model">Object corresponding to the TRIMBIM file</param>
    /// <returns cref="PSetDefinition">A collection of all the unique properties in the model.</returns>
    [Trace]
    private static IEnumerable<PSetDefinition> ProcessModelAndFetchUniquePropertyDefinitions(IModel model)
        {
            var modelProperties = model.Properties.PropertySets.PropertySetDefinitions.SelectMany(
                pSet =>
                {
                    var psetName = model.Properties.PropertySets.PropertySetNames[(int)pSet.NameId];
                    return pSet.Properties.Select(propertyDefinition =>
                    {
                        var propertyName = model.Properties.PropertySets.PropertyNames[(int)propertyDefinition.PropertyNameId];
                        return new PSetDefinition
                        {
                            PropertyName = propertyName,
                            PSetName = psetName,
                            PropertyType = propertyDefinition.Type
                        };
                    });

                }
            ).DistinctBy(x => $"{x.PSetName}{x.PropertyName}");

            var additionalProperties = ReferenceObjectPset.Properties
            .Concat([PresentationLayerPset.Property])
            .Concat(ProductPset.Properties)
            .Select(propertyString =>
            {
                var propArray = propertyString.Split(',');
                return new PSetDefinition()
                {
                    PropertyName = propArray[0],
                    PSetName = propArray[1],
                    PropertyType = PropertyType.StringValue
                };
            });

            return modelProperties.Concat(additionalProperties);
        }

    /// <inheritdoc/>
    [Trace]
    public async Task<string> GetFileDownloadUrlFromFileService(string spaceId, string fileID)
    {
        const int maxRetries = 3;
        int[] retryDelays = { 10, 15, 30 }; // Delays in seconds
        
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                return await _trimbleFileService.GetFileDownloadUrl(spaceId, fileID);
            }
            catch (Exception ex)
            {
                bool isLastAttempt = attempt == maxRetries - 1;
                
                _logger.LogWarning(
                    "Attempt {Attempt} to get file download URL failed for spaceId: {SpaceId}, fileId: {FileId}. Error: {Error}",
                    attempt + 1, spaceId, fileID, ex.Message);
                
                if (isLastAttempt)
                {
                    _logger.LogError(ex, "All retry attempts failed for getting file download URL for spaceId: {SpaceId}, fileId: {FileId}", 
                        spaceId, fileID);
                    throw;
                }
                
                await Task.Delay(TimeSpan.FromSeconds(retryDelays[attempt]));
                
                _logger.LogInformation(
                    "Retrying attempt {NextAttempt} to get file download URL for spaceId: {SpaceId}, fileId: {FileId}",
                    attempt + 2, spaceId, fileID);
            }
        }
        
        // This line should never be reached due to the throw in the last attempt
        throw new InvalidOperationException("Unexpected code path in retry logic");
    }

    #endregion
}
