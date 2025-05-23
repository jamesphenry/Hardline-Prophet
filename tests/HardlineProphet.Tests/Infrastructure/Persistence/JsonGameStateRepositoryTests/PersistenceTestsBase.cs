﻿// tests/HardlineProphet.Tests/Infrastructure/Persistence/JsonGameStateRepositoryTests/PersistenceTestsBase.cs
using HardlineProphet.Core.Models;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Xunit.Abstractions; // ITestOutputHelper

namespace HardlineProphet.Tests.Infrastructure.Persistence.JsonGameStateRepositoryTests;

/// <summary>
/// Base class for persistence tests providing common helpers and setup/teardown.
/// </summary>
public abstract class PersistenceTestsBase : IDisposable // Implement IDisposable for cleanup
{
    protected readonly ITestOutputHelper _output;
    protected readonly string _testRunDirectory; // Store the unique directory for this test instance

    // Shared serializer options for checksum calculation consistency
    protected static readonly JsonSerializerOptions _checksumSerializerOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = null
    };
    // Shared serializer options for saving test files (can differ)
    protected static readonly JsonSerializerOptions _saveTestSerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = null
    };

    protected PersistenceTestsBase(ITestOutputHelper output)
    {
        _output = output;
        _testRunDirectory = CreateTestDirectory(); // Create directory once per test class instance
    }

    // Moved Helper Methods (now protected)
    protected string GetTestDirectoryPath() => _testRunDirectory; // Return the created path

    protected string GetSaveFilePath(string username)
    {
        var sanitizedUsername = string.Join("_", username.Split(Path.GetInvalidFileNameChars()));
        if (string.IsNullOrWhiteSpace(sanitizedUsername)) { sanitizedUsername = "default_user"; }
        return Path.Combine(_testRunDirectory, $"{sanitizedUsername}.save.json");
    }

    protected string ComputeExpectedChecksum(GameState state)
    {
        var stateForHashing = state with { Checksum = null };
        var json = JsonSerializer.Serialize(stateForHashing, _checksumSerializerOptions);
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
        return Convert.ToBase64String(hashBytes);
    }

    // Helper to write raw content to a save file within the test directory
    protected async Task<string> SetupRawSaveFileAsync(string username, string jsonContent)
    {
        var saveFilePath = GetSaveFilePath(username);
        // Directory is created by constructor now
        await File.WriteAllTextAsync(saveFilePath, jsonContent);
        _output.WriteLine($"Raw save file created at: {saveFilePath}");
        return saveFilePath;
    }

    // Private method to create directory, called by constructor
    private string CreateTestDirectory()
    {
        string baseTestDir = Path.Combine(Path.GetTempPath(), "HardlineProphetTests");
        string testRunDir = Path.Combine(baseTestDir, Guid.NewGuid().ToString());
        Directory.CreateDirectory(testRunDir);
        _output.WriteLine($"Created test directory: {testRunDir}");
        return testRunDir;
    }

    // Implement IDisposable for cleanup after tests in the class run
    public void Dispose()
    {
        CleanupTestDirectory();
        GC.SuppressFinalize(this); // Prevent finalizer from running
    }

    // Cleanup method (now private, called by Dispose)
    private void CleanupTestDirectory()
    {
        if (!string.IsNullOrEmpty(_testRunDirectory) && Directory.Exists(_testRunDirectory))
        {
            _output.WriteLine($"Cleaning up test directory: {_testRunDirectory}");
            try { Directory.Delete(_testRunDirectory, true); } catch { /* Ignore cleanup errors */ }
        }
    }
}
