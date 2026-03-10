---
name: AI Security Auto-Fixer
on:
  workflow_dispatch:
---

# AI Security Auto-Fixer — Workflow Execution Instructions

## Workflow

- **File:** `.github/workflows/security-fix.yml`
- **Name:** `AI Security Auto-Fixer`
- **Trigger:** `workflow_dispatch` (manual)

## Required Parameters

| Parameter      | Required | Description                                                                 |
|----------------|----------|-----------------------------------------------------------------------------|
| `jira_key`     | Yes      | The Jira ticket key associated with the security issue (e.g. `SEC-1234`).  |
| `issue_msg`    | Yes      | A description of the security vulnerability to fix.                        |
| `file_path`    | Yes      | The relative path to the file containing the vulnerability.                |
| `line_numbers` | Yes      | The line number(s) in the file where the vulnerability is located.         |

## Execution

```bash
gh workflow run security-fix.yml \
  -f jira_key="SEC-1234" \
  -f issue_msg="SQL injection in user input handling" \
  -f file_path="src/QuantityTakeoffOrchestratorService/Repositories/SomeRepository.cs" \
  -f line_numbers="42-45"
```

## What the Workflow Does

1. **Checks out** the repository code.
2. **Installs** the GitHub Copilot CLI extension.
3. **Generates a fix** by prompting Copilot with the file path, line numbers, and issue description, then overwrites the vulnerable file with the suggested fix.
4. **Creates a pull request** on a branch named `fix/<jira_key>` with details about the fixed file, affected lines, and the issue.

