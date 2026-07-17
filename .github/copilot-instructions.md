# Copilot Instructions

## Project Guidelines
- Always write PowerShell scripts with Windows CRLF line endings. Never use Linux LF-only line endings for .ps1 files in this repository.

## File Editing Instructions
- Always use `replace_string_in_file` or `multi_replace_string_in_file` to edit existing files, and `remove_file` + `create_file` to fully replace files. 
- Never use terminal commands (Set-Content, WriteAllLines, etc.) to write or edit file content. Terminal commands should only be used for things that genuinely require them such as running builds, git operations, or executing scripts.