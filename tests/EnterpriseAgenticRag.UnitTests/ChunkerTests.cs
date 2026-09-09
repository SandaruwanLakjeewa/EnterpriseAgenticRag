using EnterpriseAgenticRag.Application;

namespace EnterpriseAgenticRag.UnitTests;

public sealed class ChunkerTests
{
    [Fact]
    public void ChunkSplitsLargeContentAndPreservesOverlap()
    {
        var chunker = new OverlappingTextChunker(maximumCharacters: 200, overlapCharacters: 30);
        string content = string.Join(' ', Enumerable.Repeat("enterprise knowledge content", 30));

        IReadOnlyList<string> chunks = chunker.Chunk(content);

        Assert.True(chunks.Count > 1);
        Assert.All(chunks, chunk => Assert.InRange(chunk.Length, 1, 200));
    }

    [Fact]
    public void ChunkReturnsNoChunksForWhitespace() =>
        Assert.Empty(new OverlappingTextChunker().Chunk("   "));
}
