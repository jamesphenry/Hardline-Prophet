// tests/HardlineProphet.Tests/Infrastructure/Persistence/JsonGameStateRepositoryTests/LoadTests.cs
using HardlineProphet.Core.Interfaces;
using HardlineProphet.Core.Models; // Access GameState, PlayerStats
using HardlineProphet.Infrastructure.Persistence;
using NFluent; // For fluent assertions
using System.IO; // For Path manipulation (optional, for cleanup)
using System.Threading.Tasks; // For Task
using Xunit; // Test framework

namespace HardlineProphet.Tests.Infrastructure.Persistence.JsonGameStateRepositoryTests;

public class LoadTests
{
    // Helper method to get a unique path for test files to avoid conflicts
    private string GetSaveFilePath(string username)
    {
        // Using a subfolder in the temp directory for test isolation
        string tempDir = Path.Combine(Path.GetTempPath(), "HardlineProphetTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir); // Ensure the directory exists
        return Path.Combine(tempDir, $"{username}.save.json");
    }

    // Cleanup method (optional but good practice for file-based tests)
    private void CleanupSaveFile(string username)
    {
        var path = GetSaveFilePath(username);
        var dir = Path.GetDirectoryName(path);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        // Clean up the test-specific directory if it exists and is empty (or force delete if needed)
        if (Directory.Exists(dir))
        {
            try { Directory.Delete(dir, true); } catch { /* Ignore cleanup errors */ }
        }
    }


    [Fact]
    public async Task LoadStateAsync_WhenSaveFileDoesNotExist_ShouldReturnNewGameStateWithDefaultValues()
    {
        // Arrange
        var username = "NewbieHacker";
        var expectedSavePath = GetSaveFilePath(username); // Get path but ensure file doesn't exist
        // Ensure the file does NOT exist before the test
        if (File.Exists(expectedSavePath))
        {
            File.Delete(expectedSavePath);
        }
        // Ensure the directory does not exist initially or is clean for this specific test instance path
        var dir = Path.GetDirectoryName(expectedSavePath);
        if (Directory.Exists(dir))
        {
            Directory.Delete(dir, true);
        }
        Directory.CreateDirectory(dir); // Create directory for the repository to potentially use

        var repository = new JsonGameStateRepository();


        // Act
        GameState loadedState = await repository.LoadStateAsync(username);


        // Assert
        // --- THESE ASSERTIONS WILL FAIL until the repository implementation exists and returns the correct object ---
        Check.That(loadedState).IsNotNull();
        Check.That(loadedState.Username).IsEqualTo(username);
        Check.That(loadedState.Level).IsEqualTo(1); // Default level 1? Or 0? Let's assume 1.
        Check.That(loadedState.Experience).IsEqualTo(0.0);
        Check.That(loadedState.Credits).IsEqualTo(100); // Default starting credits? Let's assume 100.
        Check.That(loadedState.Stats).IsNotNull();
        Check.That(loadedState.Stats.HackSpeed).IsEqualTo(5); // Default stats? Let's assume 5.
        Check.That(loadedState.Stats.Stealth).IsEqualTo(5);
        Check.That(loadedState.Stats.DataYield).IsEqualTo(0);
        Check.That(loadedState.ActiveMissionIds).IsEmpty();
        Check.That(loadedState.UnlockedPerkIds).IsEmpty();
        Check.That(loadedState.Version).IsEqualTo(2); // Should match our current GameState version
        Check.That(loadedState.Checksum).IsNull(); // No checksum for a new state
        Check.That(loadedState.IsDevSave).IsFalse(); // Default should not be dev save

        // Cleanup
        CleanupSaveFile(username); // Clean up the temp directory/file
    }

    // --- Add more tests here later for other loading scenarios ---
    // [Fact] public async Task LoadStateAsync_WhenSaveFileExistsAndIsValid_ShouldReturnLoadedState() { ... }
    // [Fact] public async Task LoadStateAsync_WhenSaveFileIsCorrupted_ShouldThrowInvalidDataException() { ... }
    // [Fact] public async Task LoadStateAsync_WhenChecksumMismatch_ShouldThrowInvalidDataException() { ... }
}
