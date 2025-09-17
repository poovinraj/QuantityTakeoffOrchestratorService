using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using NewRelic.Api.Agent;
using QuantityTakeoffOrchestratorService.Helpers;
using QuantityTakeoffOrchestratorService.Models;
using QuantityTakeoffOrchestratorService.Models.Constants;
using QuantityTakeoffOrchestratorService.Models.Domain;
using QuantityTakeoffOrchestratorService.Models.Mapping;
using QuantityTakeoffOrchestratorService.Models.View;
using QuantityTakeoffOrchestratorService.NotificationHubs;
using QuantityTakeoffOrchestratorService.Processors.Interfaces;
using QuantityTakeoffOrchestratorService.Services;
using System.Collections.Concurrent;
using System.Dynamic;
using System.Globalization;
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
    private readonly IHubContext<QuantityTakeoffOrchestratorHub> _hubContext;

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
        IHubContext<QuantityTakeoffOrchestratorHub> hubContext)
    {
        this._connectClient = connectClientService;
        this._trimbleFileService = trimbleFileService;
        _logger = logger;
        _hubContext = hubContext;
    }

    /// <summary>
    ///     This is the main method that processes the request to add a model and creates a JSON file with the model details.
    /// </summary>
    /// <param name="jobModelId"></param>
    /// <param name="modelReferecenId"></param>
    /// <param name="modelVersionId"></param>
    /// <param name="userAccessToken"></param>
    /// <param name="spaceId"></param>
    /// <param name="folderId"></param>
    /// <param name="notificationGroup"></param>
    /// <returns></returns>
    [Trace]
    public async Task<ProcessModelResult> ProcessAddModelRequestAndCreateJsonFile(string jobModelId, string modelReferecenId, string modelVersionId, string userAccessToken, string spaceId, string folderId, string notificationGroup)
    {
        try
        {
            Dictionary<string, string?> customAttributes = new() {
                        { "modelReferecenId", modelReferecenId },
                        { "modelVersionId", modelVersionId }
                    };
            // log custom attributes to NewRelic
            NewRelicHelper.AddCustomLoggingAttributes(customAttributes);


            await _hubContext.Clients.Group(notificationGroup)
                .SendAsync("ModelConversionStatus", new ConversionStatus() {
                    JobModelId = jobModelId,
                    Status = "Downloading & Processing Model", 
                    Progress = 10 });

            var model = await ProcessTrimBim(modelReferecenId, userAccessToken, modelVersionId);

            if (model is null)
            {
                var processModelFailureResult = new ProcessModelResult
                {
                    ModelId = modelReferecenId,
                    SpaceId = spaceId,
                    FolderId = folderId,
                    IsConvertedSuccessfully = false,
                    ErrorMessage = "Model is null after processing."
                };
                return processModelFailureResult;
            }

            // Elements extraction and JSON creation
            await _hubContext.Clients.Group(notificationGroup)
               .SendAsync("ModelConversionStatus", new ConversionStatus()
               {
                   JobModelId = jobModelId,
                   Status = "Element extraction in progress",
                   Progress = 50
               });
            var elementsJson = Create3DTakeoffElementsAndUploadToFileService(modelReferecenId, model);

            // Upload the JSON content to the Trimble Connect File Service
            await _hubContext.Clients.Group(notificationGroup)
               .SendAsync("ModelConversionStatus", new ConversionStatus()
               {
                   JobModelId = jobModelId,
                   Status = "Uploading Content",
                   Progress = 80
               });
            var fileId = await UploadToConnectFileService(spaceId, folderId, modelReferecenId, elementsJson);

            // Extract unique property definitions from the model

            await _hubContext.Clients.Group(notificationGroup)
               .SendAsync("ModelConversionStatus", new ConversionStatus()
               {
                   JobModelId = jobModelId,
                   Status = "Finalizing the process",
                   Progress = 90
               });

            var uniquePropertyDefinitions = ProcessModelAndFetchUniquePropertyDefinitions(model);

            var fileDownloadUrl = await GetFileDownloadUrlFromFileService(spaceId, fileId);

            // Log the successful processing of the model
            _logger.LogInformation($"Successfully processed model with ModelReferenceId: {modelReferecenId}. FileId: {fileId}");
            await _hubContext.Clients.Group(notificationGroup)
               .SendAsync("ModelConversionStatus", new ConversionStatus()
               {
                   JobModelId = jobModelId,
                   Status = "Completed",
                   Progress = 100
               });

            // Create a ProcessModelResult object to return
            var processModelResult = new ProcessModelResult
            {
                ModelId = modelReferecenId,
                SpaceId = spaceId,
                FolderId = folderId,
                FileId = fileId,
                FileDownloadUrl = fileDownloadUrl,
                UniqueProperties = uniquePropertyDefinitions.ToList(),
                IsConvertedSuccessfully = true,
            };
            return processModelResult;

        }
        catch (Exception ex)
        {
            string errorMessage = $"Failed to process the model conversion request for ModelReferenceId: {modelReferecenId}. See inner exception for details.";
            _logger.LogError(ex, errorMessage);
            throw new Exception(errorMessage, ex);
        }
    }

    private async Task<IModel?> ProcessTrimBim(string connectFileId, string userAccessToken, string? fileVersionId = null)
    {
        IModel? result = null;
        byte[]? modelBlob = await _connectClient.DownloadModelFile(userAccessToken, connectFileId, fileVersionId).ConfigureAwait(false);
        if (!Trimble.Technology.TrimBim.Model.TryParse(modelBlob, out result))
        {
            string errorMessage = "Failed to parse the model.";
            throw new Exception(errorMessage);
        }
    ;
        return result;
    }

    [Trace]
    private string Create3DTakeoffElementsAndUploadToFileService(string referenceId, IModel model)
    {
        string fileId = "";
        if (model is not null)
        {
            var propertyMappings = ProcessModelProperties(model);
            var quantityTakeoffElements = FetchBasicTakeOffElements(model, referenceId).Select(element =>
            {
                if (propertyMappings.TryGetValue(element.Model3DItemIdIndex.Idx, out var properties))
                {
                    element.Properties = properties;
                }
                return element;
            });
            List<ExpandoObject> QuantityTakeoffElements = CreateExpandoObjects(quantityTakeoffElements);
            string jsonContent = System.Text.Json.JsonSerializer.Serialize(QuantityTakeoffElements, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            return jsonContent;
        }

        return fileId;
    }

    [Trace]
    private async Task<string> UploadToConnectFileService(string spaceId, string folderId, string modelReferecenId, string jsonContent)
    {
        try
        {
            string fileId = await _trimbleFileService.UploadFileAsync(
                spaceId,
                folderId,
                modelReferecenId + ".json",
                System.Text.Encoding.UTF8.GetBytes(jsonContent)
            );

            return fileId;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error uploading takeoff elements to Trimble File Service: {ex.Message}");
            throw new Exception("Failed to upload takeoff elements to Trimble File Service.");
        }
    }

    private Dictionary<int, List<ModelProperties>> ProcessModelProperties(IModel model)
    {
        var result = new ConcurrentDictionary<int, List<ModelProperties>>();
        var lockObject = new object(); // Lock object for synchronization

        // Run the three methods in parallel
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

    private IEnumerable<QuantityTakeoffElement> FetchBasicTakeOffElements(IModel model, string ReferenceId)
    {
        return model.Geometry.Instances.Select(x => x.EntityId).Distinct()
            .Select(entityIdx =>
            {
                var entity = model.Entities.EntityInformations[(int)entityIdx];
                return new QuantityTakeoffElement
                {
                    Id = entityIdx.ToString(),
                    ReferenceId = ReferenceId,
                    ItemType = GetItemType(entity.ClassName),
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

    #region Helper methods

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
