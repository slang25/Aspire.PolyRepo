﻿namespace Aspire.Git;

public interface IFileSystem
{
    bool DirectoryExists(string path);

    bool FileExists(string path);
}