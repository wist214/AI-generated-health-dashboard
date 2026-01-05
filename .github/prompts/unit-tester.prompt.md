# Testing Agent

## Role: Test Coverage for Health Aggregator

**Focus**: Service layer testing, API endpoint validation, external service mocking

## Testing Priorities

### Unit Tests
- **Services**: Test `OuraDataService`, `PicoocDataService`, `DashboardService`
- **Data Transformation**: Verify correct mapping between API responses and domain models
- **Edge Cases**: Handle null data, empty collections, missing fields

### Integration Tests
- **API Endpoints**: Test `/api/oura/*` and `/api/picooc/*` endpoints
- **File Repository**: Verify JSON serialization/deserialization
- **Mock External APIs**: Never call real Oura/Picooc APIs in tests

## Mock Strategies

### External API Mocking
```csharp
// Mock Oura API client
var mockOuraClient = new Mock<IOuraApiClient>();
mockOuraClient
    .Setup(x => x.GetSleepDataAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
    .ReturnsAsync(new List<OuraSleepRecord> { /* test data */ });
```

### File Repository Mocking
```csharp
// Mock file repository
var mockRepo = new Mock<IDataRepository<OuraData>>();
mockRepo
    .Setup(x => x.GetAsync())
    .ReturnsAsync(testOuraData);
```

## Test Patterns

### Service Test Structure
```csharp
[Fact]
public async Task GetAllDataAsync_WhenDataExists_ReturnsData()
{
    // Arrange
    var mockRepo = new Mock<IDataRepository<OuraData>>();
    mockRepo.Setup(x => x.GetAsync()).ReturnsAsync(testData);
    var service = new OuraDataService(mockRepo.Object, mockClient.Object);

    // Act
    var result = await service.GetAllDataAsync();

    // Assert
    Assert.NotNull(result);
    Assert.Equal(expectedCount, result.SleepRecords.Count);
}
```

### JSON Serialization Tests
```csharp
[Fact]
public void Serialization_UsesCamelCase()
{
    var data = new OuraSleepRecord { TotalSleepDuration = 28800 };
    var json = JsonSerializer.Serialize(data, _camelCaseOptions);
    
    Assert.Contains("totalSleepDuration", json);
    Assert.DoesNotContain("total_sleep_duration", json);
}
```

## Quality Standards

- Test all public service methods
- Verify JSON property naming in serialized output
- Mock all external dependencies
- Use meaningful test names describing behavior
