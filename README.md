# Autosupport LSP Server

## How to use this

### When creating a new LSP client

Any LSP client should start this server over the commandline with the definition file as it's only argument (e.g. `dotnet "path/to/autosupport-lsp-server.dll" "path/to/definitionFile.def"`)

### When cloning this repository

This repository makes use of an git submodule.
Make sure to use the according git commands.

Most notably `git clone --recurse-submodules` to clone or `git submodule update --init` if you've already cloned it.
