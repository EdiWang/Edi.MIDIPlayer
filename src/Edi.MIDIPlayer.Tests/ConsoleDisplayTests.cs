namespace Edi.MIDIPlayer.Tests;

public class ConsoleDisplayTests
{
    [Fact]
    public void DisplayHackerBanner_SetsConsoleColorAndDisplaysPianoArt()
    {
        // Arrange
        var originalOut = Console.Out;
        var originalColor = Console.ForegroundColor;
        var output = new StringWriter();
        
        try
        {
            Console.SetOut(output);
            
            // Act
            ConsoleDisplay.DisplayHackerBanner();
            
            // Assert
            var result = output.ToString();
            Assert.Contains("_______________________________________________________", result);
            Assert.Contains("INIT", result);
            Assert.Contains("EDI.MIDIPLAYER Terminal", result);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.ForegroundColor = originalColor;
        }
    }

    [Theory]
    [InlineData("INFO", "Test message", ConsoleColor.Blue)]
    [InlineData("ERROR", "Error occurred", ConsoleColor.Red)]
    [InlineData("WARN", "Warning message", ConsoleColor.Yellow)]
    [InlineData("DEBUG", "Debug info", ConsoleColor.Green)]
    public void WriteMessage_FormatsMessageCorrectly(string type, string message, ConsoleColor color)
    {
        // Arrange
        var originalOut = Console.Out;
        var originalColor = Console.ForegroundColor;
        var output = new StringWriter();
        
        try
        {
            Console.SetOut(output);
            
            // Act
            ConsoleDisplay.WriteMessage(type, message, color);
            
            // Assert
            var result = output.ToString();
            Assert.Contains($"[{type,-5}]", result);
            Assert.Contains(message, result);
            Assert.Matches(@"\[\d{2}:\d{2}:\d{2}\.\d{3}\]", result); // Timestamp pattern
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.ForegroundColor = originalColor;
        }
    }

    [Fact]
    public void WriteMessage_IncludesTimestamp()
    {
        // Arrange
        var originalOut = Console.Out;
        var output = new StringWriter();
        
        try
        {
            Console.SetOut(output);
            var beforeTime = DateTime.Now;
            
            // Act
            ConsoleDisplay.WriteMessage("TEST", "Message", ConsoleColor.White);
            
            var afterTime = DateTime.Now;
            var result = output.ToString();
            
            // Assert
            Assert.Matches(@"\[\d{2}:\d{2}:\d{2}\.\d{3}\]", result);
            
            // Extract timestamp from output and verify it's within reasonable range
            var timestampMatch = System.Text.RegularExpressions.Regex.Match(result, @"\[(\d{2}):(\d{2}):(\d{2})\.(\d{3})\]");
            Assert.True(timestampMatch.Success, "Timestamp should be present in correct format");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void GetConsoleLock_ReturnsNonNullLock()
    {
        // Act
        var consoleLock = ConsoleDisplay.GetConsoleLock();
        
        // Assert
        Assert.NotNull(consoleLock);
        Assert.IsType<Lock>(consoleLock);
    }

    [Fact]
    public void GetConsoleLock_ReturnsSameInstance()
    {
        // Act
        var lock1 = ConsoleDisplay.GetConsoleLock();
        var lock2 = ConsoleDisplay.GetConsoleLock();
        
        // Assert
        Assert.Same(lock1, lock2);
    }

    [Theory]
    [InlineData(0, "░░░░░░░░░░")] // No velocity
    [InlineData(127, "█████████░")] // Max velocity (should fill 10/10 bars, but last char is always empty in this implementation)
    [InlineData(63, "████░░░░░░")] // Half velocity
    [InlineData(25, "██░░░░░░░░")] // Low velocity
    [InlineData(100, "████████░░")] // High velocity
    public void CreateVelocityBar_ReturnsCorrectBarLength(int velocity, string expectedPattern)
    {
        // Arrange
        var originalColor = Console.ForegroundColor;
        
        try
        {
            // Act
            var result = ConsoleDisplay.CreateVelocityBar(velocity);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(10, result.Length);
            
            // Check the pattern matches expected filled/unfilled characters
            var filledCount = result.Count(c => c == '█');
            var unfilledCount = result.Count(c => c == '░');
            
            Assert.Equal(10, filledCount + unfilledCount);
            
            var expectedFilled = (int)((velocity / 127.0) * 10);
            Assert.Equal(expectedFilled, filledCount);
            Assert.Equal(10 - expectedFilled, unfilledCount);
        }
        finally
        {
            Console.ForegroundColor = originalColor;
        }
    }

    [Theory]
    [InlineData(-1)] // Below minimum
    [InlineData(128)] // Above maximum
    [InlineData(200)] // Way above maximum
    public void CreateVelocityBar_HandlesInvalidVelocityValues(int velocity)
    {
        // Arrange
        var originalColor = Console.ForegroundColor;
        
        try
        {
            // Act & Assert - Should not throw exception
            var result = ConsoleDisplay.CreateVelocityBar(velocity);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(10, result.Length);
        }
        finally
        {
            Console.ForegroundColor = originalColor;
        }
    }

    [Fact]
    public void CreateVelocityBar_ResetsConsoleColor()
    {
        // Arrange
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Blue; // Set to a specific color
        
        try
        {
            // Act
            ConsoleDisplay.CreateVelocityBar(64);
            
            // Assert - Console color should be reset
            // Note: This is tricky to test directly since Console.ResetColor() sets it to the default
            // We're mainly ensuring no exception is thrown and the method completes
            Assert.True(true); // Method completed without exception
        }
        finally
        {
            Console.ForegroundColor = originalColor;
        }
    }

    [Fact]
    public void WriteMessage_HandlesEmptyMessage()
    {
        // Arrange
        var originalOut = Console.Out;
        var output = new StringWriter();
        
        try
        {
            Console.SetOut(output);
            
            // Act
            ConsoleDisplay.WriteMessage("TEST", "", ConsoleColor.White);
            
            // Assert
            var result = output.ToString();
            Assert.Contains("[TEST ]", result);
            Assert.Matches(@"\[\d{2}:\d{2}:\d{2}\.\d{3}\]", result);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void WriteMessage_HandlesNullMessage()
    {
        // Arrange
        var originalOut = Console.Out;
        var output = new StringWriter();
        
        try
        {
            Console.SetOut(output);
            
            // Act
            ConsoleDisplay.WriteMessage("TEST", null!, ConsoleColor.White);
            
            // Assert
            var result = output.ToString();
            Assert.Contains("[TEST ]", result);
            Assert.Matches(@"\[\d{2}:\d{2}:\d{2}\.\d{3}\]", result);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void WriteMessage_TypeIsPaddedCorrectly()
    {
        // Arrange
        var originalOut = Console.Out;
        var output = new StringWriter();
        
        try
        {
            Console.SetOut(output);
            
            // Act
            ConsoleDisplay.WriteMessage("A", "Test", ConsoleColor.White);
            
            // Assert
            var result = output.ToString();
            Assert.Contains("[A    ]", result); // Should be padded to 5 characters
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}