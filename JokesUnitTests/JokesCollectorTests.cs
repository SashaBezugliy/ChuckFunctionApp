using ChuckFunctionApp.Infrastructure.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace JokesUnitTests

{
    public class JokesServiceTests
    {
        [Fact]
        public async Task CollectJokes_InsertsOnlyValidJokes_LogsCorrectly()
        {
            // Arrange
            var mockProvider = new Mock<IJokesAPI>();
            var mockRepo = new Mock<IJokesRepository>();
            var mockLogger = new Mock<ILogger<JokesCollector>>();

            var jokesFromApi = new List<Joke>
        {
            new Joke { Text = "Short joke" },  // valid
            new Joke { Text = "" },             // empty, should be filtered
            new Joke { Text = new string('x', 250) } // too long, filtered
        };

            mockProvider.Setup(p => p.GetAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(jokesFromApi);

            // InsertJokeAsync returns 1 for valid joke
            mockRepo.Setup(r => r.InsertJokeAsync(It.Is<Joke>(j => j.Text == "Short joke")))
                .ReturnsAsync(1);

            var service = new JokesCollector(mockProvider.Object, mockRepo.Object, mockLogger.Object);

            // Act
            await service.CollectJokes(3, CancellationToken.None);

            // Assert
            // InsertJokeAsync is called only once
            mockRepo.Verify(r => r.InsertJokeAsync(It.IsAny<Joke>()), Times.Once);

            // Logging with Error is never called
            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Never);
        }

        [Fact]
        public async Task CollectJokes_WhenInsertThrowsError_IncrementsFailedCountAndLogsError()
        {
            // Arrange
            var mockProvider = new Mock<IJokesAPI>();
            var mockRepo = new Mock<IJokesRepository>();
            var mockLogger = new Mock<ILogger<JokesCollector>>();

            var jokesFromApi = new List<Joke>
        {
            new Joke { Text = "Short joke" }
        };

            mockProvider.Setup(p => p.GetAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(jokesFromApi);

            // Simulate DB failure
            mockRepo.Setup(r => r.InsertJokeAsync(It.IsAny<Joke>()))
                .ThrowsAsync(new Exception("DB error"));

            var service = new JokesCollector(mockProvider.Object, mockRepo.Object, mockLogger.Object);

            // Act
            await service.CollectJokes(1, CancellationToken.None);

            // Assert
            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to insert joke")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);

            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to insert: 1")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task CollectJokes_WhenApiReturnsEmpty_NoDbCallsAndLogsCorrectly()
        {
            // Arrange
            var mockProvider = new Mock<IJokesAPI>();
            var mockRepo = new Mock<IJokesRepository>();
            var mockLogger = new Mock<ILogger<JokesCollector>>();

            // API вертає пустий список
            mockProvider.Setup(p => p.GetAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Joke>());

            var service = new JokesCollector(mockProvider.Object, mockRepo.Object, mockLogger.Object);

            // Act
            await service.CollectJokes(5, CancellationToken.None);

            // Assert
            // No database call
            mockRepo.Verify(r => r.InsertJokeAsync(It.IsAny<Joke>()), Times.Never);

            // Correct Info message is logged
            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) =>
                        v.ToString().Contains("Fetched from API: 0, Filtered: 0, Inserted: 0, Failed to insert: 0")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
}