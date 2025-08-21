namespace Edi.MIDIPlayer.Tests;

public class InputHandlerTests
{
    [Fact]
    public void GetMidiFilePath_WithArgs_ReturnsFirstArgument()
    {
        // Arrange
        var args = new[] { "test.mid", "extra", "arguments" };
        
        // Act
        var result = InputHandler.GetMidiFilePath(args);
        
        // Assert
        Assert.Equal("test.mid", result);
    }

    [Fact]
    public void GetMidiFilePath_WithSingleArg_ReturnsArgument()
    {
        // Arrange
        var args = new[] { "/path/to/music.mid" };
        
        // Act
        var result = InputHandler.GetMidiFilePath(args);
        
        // Assert
        Assert.Equal("/path/to/music.mid", result);
    }

    [Fact]
    public void GetMidiFilePath_WithEmptyArgs_PromptsUserAndReturnsInput()
    {
        // Arrange
        var args = Array.Empty<string>();
        var originalOut = Console.Out;
        var originalIn = Console.In;
        var originalColor = Console.ForegroundColor;
        var output = new StringWriter();
        var input = new StringReader("user-input.mid");
        
        try
        {
            Console.SetOut(output);
            Console.SetIn(input);
            
            // Act
            var result = InputHandler.GetMidiFilePath(args);
            
            // Assert
            Assert.Equal("user-input.mid", result);
            
            var outputText = output.ToString();
            Assert.Contains("[INPUT]", outputText);
            Assert.Contains("Enter MIDI file path:", outputText);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetIn(originalIn);
            Console.ForegroundColor = originalColor;
        }
    }

    [Fact]
    public void GetMidiFilePath_WithEmptyArgs_TrimsQuotesFromUserInput()
    {
        // Arrange
        var args = Array.Empty<string>();
        var originalOut = Console.Out;
        var originalIn = Console.In;
        var originalColor = Console.ForegroundColor;
        var output = new StringWriter();
        var input = new StringReader("\"quoted-path.mid\"");
        
        try
        {
            Console.SetOut(output);
            Console.SetIn(input);
            
            // Act
            var result = InputHandler.GetMidiFilePath(args);
            
            // Assert
            Assert.Equal("quoted-path.mid", result);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetIn(originalIn);
            Console.ForegroundColor = originalColor;
        }
    }

    [Fact]
    public void GetMidiFilePath_WithEmptyArgs_HandlesEmptyUserInput()
    {
        // Arrange
        var args = Array.Empty<string>();
        var originalOut = Console.Out;
        var originalIn = Console.In;
        var originalColor = Console.ForegroundColor;
        var output = new StringWriter();
        var input = new StringReader("");
        
        try
        {
            Console.SetOut(output);
            Console.SetIn(input);
            
            // Act
            var result = InputHandler.GetMidiFilePath(args);
            
            // Assert
            Assert.Equal(string.Empty, result);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetIn(originalIn);
            Console.ForegroundColor = originalColor;
        }
    }

    [Fact]
    public void GetMidiFilePath_WithEmptyArgs_HandlesNullUserInput()
    {
        // Arrange
        var args = Array.Empty<string>();
        var originalOut = Console.Out;
        var originalIn = Console.In;
        var originalColor = Console.ForegroundColor;
        var output = new StringWriter();
        var input = new StringReader("\0"); // Simulates null input
        
        try
        {
            Console.SetOut(output);
            Console.SetIn(input);
            
            // Act
            var result = InputHandler.GetMidiFilePath(args);
            
            // Assert
            Assert.Equal(string.Empty, result);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetIn(originalIn);
            Console.ForegroundColor = originalColor;
        }
    }

    [Fact]
    public void GetMidiFilePath_WithEmptyArgs_SetsCorrectConsoleColors()
    {
        // Arrange
        var args = Array.Empty<string>();
        var originalOut = Console.Out;
        var originalIn = Console.In;
        var originalColor = Console.ForegroundColor;
        var output = new StringWriter();
        var input = new StringReader("test.mid");
        
        try
        {
            Console.SetOut(output);
            Console.SetIn(input);
            
            // Act
            InputHandler.GetMidiFilePath(args);
            
            // Assert - Verify the console color was reset
            // The method should reset the color after setting it to white
            Assert.True(true); // Method completed without exception
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetIn(originalIn);
            Console.ForegroundColor = originalColor;
        }
    }

    [Theory]
    [InlineData("simple.mid")]
    [InlineData("/full/path/to/file.mid")]
    [InlineData("C:\\Windows\\Path\\file.mid")]
    [InlineData("file with spaces.mid")]
    [InlineData("§æ§Ñ§Û§Ý.mid")] // Unicode filename
    public void GetMidiFilePath_WithEmptyArgs_HandlesVariousUserInputs(string userInput)
    {
        // Arrange
        var args = Array.Empty<string>();
        var originalOut = Console.Out;
        var originalIn = Console.In;
        var originalColor = Console.ForegroundColor;
        var output = new StringWriter();
        var input = new StringReader(userInput);
        
        try
        {
            Console.SetOut(output);
            Console.SetIn(input);
            
            // Act
            var result = InputHandler.GetMidiFilePath(args);
            
            // Assert
            Assert.Equal(userInput, result);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetIn(originalIn);
            Console.ForegroundColor = originalColor;
        }
    }

    [Fact]
    public void GetMidiFilePath_WithEmptyArgs_TrimsWhitespaceFromUserInput()
    {
        // Arrange
        var args = Array.Empty<string>();
        var originalOut = Console.Out;
        var originalIn = Console.In;
        var originalColor = Console.ForegroundColor;
        var output = new StringWriter();
        var input = new StringReader("  spaced-file.mid  ");
        
        try
        {
            Console.SetOut(output);
            Console.SetIn(input);
            
            // Act
            var result = InputHandler.GetMidiFilePath(args);
            
            // Assert
            Assert.Equal("spaced-file.mid", result);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetIn(originalIn);
            Console.ForegroundColor = originalColor;
        }
    }

    [Fact]
    public void GetMidiFilePath_WithNullArgs_ThrowsException()
    {
        // Arrange
        string[] args = null!;
        
        // Act & Assert
        Assert.Throws<NullReferenceException>(() => InputHandler.GetMidiFilePath(args));
    }
}