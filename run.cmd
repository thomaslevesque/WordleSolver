@echo off
pushd %~dp0
dotnet run --project WordleSolver %*
popd
