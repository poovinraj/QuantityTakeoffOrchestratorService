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
---

# Instructions

You are an AI security engineer. A security vulnerability has been identified in the codebase.

1. Read the vulnerability description: ${{ inputs.issue_msg }}.
2. Open the file at `${{ inputs.file_path }}` and navigate to line(s) ${{ inputs.line_numbers }}.
3. Analyze the code at those lines and implement a fix for the described security issue.
4. Create a new local branch named `fix/${{ inputs.jira_key }}`.
5. git add and git commit your changes with a meaningful message referencing ${{ inputs.jira_key }}.
6. Use the create_pull_request tool to open a Pull Request.
7. Finally, run the following command exactly to notify n8n that execution is complete, replacing `<PR_URL>` with the actual Pull Request URL from step 6:
   ```bash
   curl -X POST "${{ inputs.resumeUrl }}" \
     -H "Content-Type: application/json" \
     -d '{"status": "completed", "result": "success", "pull_request_url": "<PR_URL>"}'
   ```
   This step must always run, even if previous steps fail.

