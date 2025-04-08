using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using TabletopTunes.Core.Common;
using TabletopTunes.Core.Entities;

namespace TabletopTunes.Tests
{
    public class TrackQueryParserTests
    {
        [Fact]
        public void ParseQuery_EmptyQuery_ReturnsAllTracks()
        {
            // Arrange
            var parser = new TrackQueryParser();
            
            // Act
            var predicate = parser.ParseQuery("");
            
            // Assert
            Assert.NotNull(predicate);
            
            // Test with a sample track - should return true for any track
            var track = new TrackEntity { Title = "Sample Track" };
            var result = predicate.Compile()(track);
            Assert.True(result);
        }
        
        [Fact]
        public void ParseQuery_TextQuery_MatchesTitle()
        {
            // Arrange
            var parser = new TrackQueryParser();
            
            // Act
            var predicate = parser.ParseQuery("rock");
            
            // Assert
            Assert.NotNull(predicate);
            
            // Should match a track with "rock" in the title
            var matchingTrack = new TrackEntity { Title = "Rock Anthem" };
            var nonMatchingTrack = new TrackEntity { Title = "Jazz Song" };
            
            var compiledPredicate = predicate.Compile();
            Assert.True(compiledPredicate(matchingTrack));
            Assert.False(compiledPredicate(nonMatchingTrack));
        }
        
        [Fact]
        public void ParseQuery_TagQuery_MatchesTag()
        {
            // Arrange
            var parser = new TrackQueryParser();
            
            // Act
            var predicate = parser.ParseQuery("#rock");
            
            // Assert
            Assert.NotNull(predicate);
            
            // Create a track with a rock tag
            var rockTag = new Tag { Id = 1, Name = "rock" };
            var matchingTrack = new TrackEntity
            {
                Title = "Song Title",
                TrackTags = new List<TrackTag>
                {
                    new TrackTag { Tag = rockTag }
                }
            };
            
            // Create a track without a rock tag
            var nonMatchingTrack = new TrackEntity
            {
                Title = "Another Song",
                TrackTags = new List<TrackTag>
                {
                    new TrackTag { Tag = new Tag { Id = 2, Name = "jazz" } }
                }
            };
            
            var compiledPredicate = predicate.Compile();
            Assert.True(compiledPredicate(matchingTrack));
            Assert.False(compiledPredicate(nonMatchingTrack));
        }
        
        [Fact]
        public void ParseQuery_ComplexQuery_EvaluatesCorrectly()
        {
            // Arrange
            var parser = new TrackQueryParser();
            
            // Act - rock AND (upbeat OR energetic) AND NOT slow
            var predicate = parser.ParseQuery("#rock & (#upbeat | #energetic) & !#slow");
            
            // Assert
            Assert.NotNull(predicate);
            
            // Create tags
            var rockTag = new Tag { Id = 1, Name = "rock" };
            var upbeatTag = new Tag { Id = 2, Name = "upbeat" };
            var energeticTag = new Tag { Id = 3, Name = "energetic" };
            var slowTag = new Tag { Id = 4, Name = "slow" };
            
            // Test cases
            var track1 = CreateTrackWithTags("Track 1", new[] { rockTag, upbeatTag }); // Should match
            var track2 = CreateTrackWithTags("Track 2", new[] { rockTag, energeticTag }); // Should match
            var track3 = CreateTrackWithTags("Track 3", new[] { rockTag, slowTag }); // Should NOT match
            var track4 = CreateTrackWithTags("Track 4", new[] { rockTag, upbeatTag, slowTag }); // Should NOT match
            var track5 = CreateTrackWithTags("Track 5", new[] { rockTag }); // Should NOT match (missing upbeat or energetic)
            
            var compiledPredicate = predicate.Compile();
            Assert.True(compiledPredicate(track1));
            Assert.True(compiledPredicate(track2));
            Assert.False(compiledPredicate(track3));
            Assert.False(compiledPredicate(track4));
            Assert.False(compiledPredicate(track5));
        }
        
        private TrackEntity CreateTrackWithTags(string title, IEnumerable<Tag> tags)
        {
            var track = new TrackEntity
            {
                Title = title,
                TrackTags = new List<TrackTag>()
            };
            
            foreach (var tag in tags)
            {
                track.TrackTags.Add(new TrackTag { Tag = tag });
            }
            
            return track;
        }
    }
}