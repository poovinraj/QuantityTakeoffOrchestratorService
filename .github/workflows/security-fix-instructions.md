---
name: AI Security Auto-Fixer
# Add an empty 'on' trigger to satisfy the GitHub Actions compiler
on:
  workflow_dispatch:
    inputs:
      jira_key:
        description: "Jira Issue Key"
        required: true
        type: string
      issue_msg:
        description: "Description of the security vulnerability"
        required: true
        type: string
      file_path:
        description: "Relative path to the file containing the vulnerability"
        required: true
        type: string
      line_numbers:
        description: "Line number(s) where the vulnerability is located"
        required: true
        type: string
      resumeUrl:
        description: "The n8n webhook callback URL"
        required: true
        type: string

description: Fixes a security vulnerability reported in Jira in the specified file
engine: copilot
permissions:
  contents: read
  pull-requests: read
safe-outputs:
  create-pull-request: {}
  allowed-domains:
    - "flows-webhook.stage.trimble-ai.com"
---

# Instructions

You are an AI security engineer. A security vulnerability has been identified in the codebase.

1. Read the vulnerability description: ${{ inputs.issue_msg }}.
2. Open the file at `${{ inputs.file_path }}` and navigate to line(s) ${{ inputs.line_numbers }}.
3. Analyze the code at those lines and implement a fix for the described security issue.
4. Create a new local branch named `fix/${{ inputs.jira_key }}`.
5. git add and git commit your changes with a meaningful message referencing ${{ inputs.jira_key }}.
6. Use the create_pull_request tool to open a Pull Request.
7. Extract the Pull Request URL from the `create_pull_request` tool output in step 6. Then immediately execute this exact shell command — substitute the real PR URL inline, do not use a placeholder, do not echo it:
   ```bash
   curl -X POST "${{ inputs.resumeUrl }}" -H "Content-Type: application/json" -d "{\"status\": \"completed\", \"result\": \"success\", \"pull_request_url\": \"ACTUAL_PR_URL_HERE\"}"
   ```
   Replace `ACTUAL_PR_URL_HERE` with the real URL before running. Confirm the curl exit code is 0.

