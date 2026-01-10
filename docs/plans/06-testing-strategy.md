# Comprehensive Testing Strategy Plan

## 1. Overview

This document outlines the comprehensive testing strategy for the Health Aggregator system, covering **E2E testing with Playwright for .NET API**, **UI testing with Playwright for React SPA**, **unit testing**, **integration testing**, and **performance testing**, following Microsoft best practices for 2024-2026.

### 1.1 Testing Goals

- **Quality Assurance**: 80%+ code coverage with meaningful tests
- **Fast Feedback**: Quick test execution for CI/CD integration
- **Reliability**: Stable tests that don't flake
- **Maintainability**: Clear test structure and documentation
- **Isolation**: Tests don't interfere with each other
- **Realistic**: Production-like test environments

### 1.2 Testing Pyramid

```
         /\
        /  \  E2E Tests (Playwright)
       /____\  10%
      /      \
     / Integ. \ Integration Tests
    /  Tests   \ 20%
   /____________\
  /              \
 /  Unit Tests    \ Unit Tests (Vitest, xUnit)
/__________________\ 70%
```

### 1.3 Technology Stack

- **E2E Testing**: Playwright for .NET (API) and TypeScript (UI)
- **Unit Testing**: xUnit (.NET), Vitest (React)
- **Mocking**: Moq (.NET), MSW (React)
- **Test Data**: EF Core In-Memory Database, Test Fixtures
- **CI/CD**: GitHub Actions with test parallelization
- **Coverage**: Coverlet (.NET), Vitest Coverage (React)
- **Performance**: BenchmarkDotNet, Lighthouse CI

---

## 2. Testing Project Locations

**IMPORTANT:** All test projects should be created for the **V2** refactored system in the `HealthAggregatorV2/` folder structure.

**Test Project Locations:**
```
HealthAggregatorV2/tests/
├── Api.Tests/           # Unit tests for V2 API
├── Api.E2E/             # E2E tests for V2 API endpoints
├── Functions.Tests/     # Unit tests for V2 Functions
├── Spa.Tests/           # Unit tests for React components
└── Spa.E2E/             # E2E tests for React UI
```

**Existing V1 System:** The `HealthAggregatorApi/` folder remains untouched. Tests are only for the V2 implementation.

**Note:** During migration, you may want to run comparison tests between V1 and V2 systems to verify feature parity.

---

## 3. Unit Testing Strategy

### 3.1 .NET Unit Tests with xUnit

#### 2.1.1 Service Layer Tests

```csharp
// Tests/Unit/Services/MetricsServiceTests.cs
public class MetricsServiceTests
{
    private readonly Mock<IMetricsRepository> _mockRepository;
    private readonly Mock<ILogger<MetricsService>> _mockLogger;
    private readonly MetricsService _service;

    public MetricsServiceTests()
    {
        _mockRepository = new Mock<IMetricsRepository>();
        _mockLogger = new Mock<ILogger<MetricsService>>();
        _service = new MetricsService(_mockRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetLatestMetricsAsync_ReturnsLatestMeasurements()
    {
        // Arrange
        var expectedMeasurements = new[]
        {
            new Measurement
            {
                Id = 1,
                MetricType = new MetricType { Name = "sleep_score" },
                Value = 85,
                Timestamp = DateTime.UtcNow
            }
        };

        _mockRepository
            .Setup(r => r.GetLatestByTypeAsync())
            .ReturnsAsync(expectedMeasurements);

        // Act
        var result = await _service.GetLatestMetricsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Metrics);
        Assert.Equal(85, result.Metrics.First().Value);
        _mockRepository.Verify(r => r.GetLatestByTypeAsync(), Times.Once);
    }

    [Theory]
    [InlineData("sleep_score", 0, 100, true)]
    [InlineData("sleep_score", -10, 100, false)]
    [InlineData("sleep_score", 0, 150, false)]
    public void ValidateMetricValue_ReturnsExpectedResult(
        string metricType,
        decimal value,
        decimal maxValue,
        bool expectedValid)
    {
        // Act
        var isValid = _service.ValidateMetricValue(metricType, value, maxValue);

        // Assert
        Assert.Equal(expectedValid, isValid);
    }
}
```

#### 2.1.2 Repository Tests with In-Memory Database

```csharp
// Tests/Unit/Repositories/MeasurementsRepositoryTests.cs
public class MeasurementsRepositoryTests : IDisposable
{
    private readonly HealthDbContext _context;
    private readonly MeasurementsRepository _repository;

    public MeasurementsRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<HealthDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new HealthDbContext(options);
        _repository = new MeasurementsRepository(_context, Mock.Of<ILogger<MeasurementsRepository>>());

        SeedTestData();
    }

    private void SeedTestData()
    {
        var metricType = new MetricType
        {
            Id = 1,
            Name = "sleep_score",
            Unit = "score",
            Category = "Sleep"
        };

        var source = new Source
        {
            Id = 1,
            ProviderName = "Oura",
            IsEnabled = true
        };

        _context.MetricTypes.Add(metricType);
        _context.Sources.Add(source);
        _context.SaveChanges();

        var measurements = new[]
        {
            new Measurement
            {
                MetricTypeId = 1,
                SourceId = 1,
                Value = 80,
                Timestamp = DateTime.UtcNow.AddDays(-3)
            },
            new Measurement
            {
                MetricTypeId = 1,
                SourceId = 1,
                Value = 85,
                Timestamp = DateTime.UtcNow.AddDays(-2)
            },
            new Measurement
            {
                MetricTypeId = 1,
                SourceId = 1,
                Value = 90,
                Timestamp = DateTime.UtcNow.AddDays(-1)
            }
        };

        _context.Measurements.AddRange(measurements);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetLatestByMetricTypeAsync_ReturnsLatestMeasurement()
    {
        // Act
        var result = await _repository.GetLatestByMetricTypeAsync("sleep_score");

        // Assert
        Assert.NotNull(result);
        var latest = Assert.Single(result);
        Assert.Equal(90, latest.Value);
    }

    [Fact]
    public async Task GetByMetricTypeInRangeAsync_ReturnsFilteredMeasurements()
    {
        // Arrange
        var from = DateTime.UtcNow.AddDays(-3);
        var to = DateTime.UtcNow.AddDays(-1);

        // Act
        var result = await _repository.GetByMetricTypeInRangeAsync("sleep_score", from, to);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task ExistsAsync_WhenDuplicateExists_ReturnsTrue()
    {
        // Arrange
        var timestamp = DateTime.UtcNow.AddDays(-1);

        // Act
        var exists = await _repository.ExistsAsync(1, 1, timestamp);

        // Assert
        Assert.True(exists);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
```

### 2.2 React Unit Tests with Vitest

#### 2.2.1 Component Tests

```typescript
// src/features/dashboard/components/DashboardCard.test.tsx
import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { DashboardCard } from './DashboardCard';

const renderWithProviders = (ui: React.ReactElement) => {
  return render(<FluentProvider theme={webLightTheme}>{ui}</FluentProvider>);
};

describe('DashboardCard', () => {
  it('renders title and value correctly', () => {
    renderWithProviders(
      <DashboardCard title="Sleep Score" value={85} unit="score" />
    );

    expect(screen.getByText('Sleep Score')).toBeInTheDocument();
    expect(screen.getByText('85')).toBeInTheDocument();
    expect(screen.getByText('score')).toBeInTheDocument();
  });

  it('displays placeholder when value is null', () => {
    renderWithProviders(
      <DashboardCard title="Sleep Score" value={null} unit="score" />
    );

    expect(screen.getByText('--')).toBeInTheDocument();
  });

  it('formats large numbers with commas', () => {
    renderWithProviders(
      <DashboardCard title="Steps" value={12345} unit="steps" />
    );

    expect(screen.getByText('12,345')).toBeInTheDocument();
  });
});
```

#### 2.2.2 Hook Tests

```typescript
// src/features/dashboard/hooks/useDashboardData.test.ts
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderHook, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { useDashboardData } from './useDashboardData';
import { dashboardService } from '../services/dashboardService';

// Mock the service
vi.mock('../services/dashboardService');

const createWrapper = () => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
    },
  });

  return ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
};

describe('useDashboardData', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('fetches dashboard data successfully', async () => {
    const mockData = {
      sleepScore: 85,
      readinessScore: 78,
      activityScore: 92,
      steps: 10000,
      weight: 75.5,
      caloriesBurned: 2500,
      lastUpdated: '2026-01-10T12:00:00Z',
    };

    vi.mocked(dashboardService.getSummary).mockResolvedValue(mockData);

    const { result } = renderHook(() => useDashboardData(), {
      wrapper: createWrapper(),
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    expect(result.current.data).toEqual(mockData);
    expect(dashboardService.getSummary).toHaveBeenCalledTimes(1);
  });

  it('handles errors gracefully', async () => {
    vi.mocked(dashboardService.getSummary).mockRejectedValue(
      new Error('API Error')
    );

    const { result } = renderHook(() => useDashboardData(), {
      wrapper: createWrapper(),
    });

    await waitFor(() => expect(result.current.isError).toBe(true));

    expect(result.current.error).toEqual(new Error('API Error'));
  });
});
```

#### 2.2.3 Utility Function Tests

```typescript
// src/shared/utils/formatters.test.ts
import { describe, it, expect } from 'vitest';
import { formatNumber, formatPercentage, formatWeight } from './formatters';

describe('formatters', () => {
  describe('formatNumber', () => {
    it('formats number with default decimals', () => {
      expect(formatNumber(1234)).toBe('1,234');
    });

    it('formats number with specified decimals', () => {
      expect(formatNumber(1234.567, 2)).toBe('1,234.57');
    });

    it('returns placeholder for null', () => {
      expect(formatNumber(null)).toBe('--');
    });
  });

  describe('formatPercentage', () => {
    it('formats percentage correctly', () => {
      expect(formatPercentage(85.678)).toBe('85.7%');
    });

    it('returns placeholder for null', () => {
      expect(formatPercentage(null)).toBe('--');
    });
  });

  describe('formatWeight', () => {
    it('formats weight with one decimal', () => {
      expect(formatWeight(75.456)).toBe('75.5 kg');
    });

    it('returns placeholder for null', () => {
      expect(formatWeight(null)).toBe('--');
    });
  });
});
```

---

## 3. Integration Testing Strategy

### 3.1 .NET Integration Tests with WebApplicationFactory

```csharp
// Tests/Integration/MetricsApiIntegrationTests.cs
public class MetricsApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public MetricsApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the real DbContext
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<HealthDbContext>));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database
                services.AddDbContext<HealthDbContext>(options =>
                {
                    options.UseInMemoryDatabase("IntegrationTestDb");
                });

                // Seed test data
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<HealthDbContext>();
                SeedTestData(db);
            });
        });

        _client = _factory.CreateClient();
    }

    private static void SeedTestData(HealthDbContext context)
    {
        context.MetricTypes.Add(new MetricType
        {
            Id = 1,
            Name = "sleep_score",
            Unit = "score",
            Category = "Sleep"
        });

        context.Sources.Add(new Source
        {
            Id = 1,
            ProviderName = "Oura",
            IsEnabled = true
        });

        context.Measurements.Add(new Measurement
        {
            MetricTypeId = 1,
            SourceId = 1,
            Value = 85,
            Timestamp = DateTime.UtcNow
        });

        context.SaveChanges();
    }

    [Fact]
    public async Task GetLatestMetrics_ReturnsOkWithData()
    {
        // Act
        var response = await _client.GetAsync("/api/metrics/latest");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());

        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("sleep_score", json);
    }

    [Fact]
    public async Task GetMetricsInRange_WithValidDates_ReturnsMetrics()
    {
        // Arrange
        var from = DateTime.UtcNow.AddDays(-7).ToString("o");
        var to = DateTime.UtcNow.ToString("o");

        // Act
        var response = await _client.GetAsync($"/api/metrics/range?from={from}&to={to}");

        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetMetricsInRange_WithMissingParameters_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/metrics/range");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }
}
```

### 3.2 React Integration Tests with MSW

```typescript
// src/tests/mocks/handlers.ts
import { http, HttpResponse } from 'msw';

export const handlers = [
  http.get('/api/dashboard/summary', () => {
    return HttpResponse.json({
      sleepScore: 85,
      readinessScore: 78,
      activityScore: 92,
      steps: 10000,
      weight: 75.5,
      caloriesBurned: 2500,
      lastUpdated: '2026-01-10T12:00:00Z',
    });
  }),

  http.get('/api/metrics/latest', () => {
    return HttpResponse.json({
      metrics: [
        {
          metricType: 'sleep_score',
          value: 85,
          unit: 'score',
          timestamp: '2026-01-10T08:00:00Z',
          sourceName: 'Oura',
        },
      ],
    });
  }),

  http.get('/api/sources', () => {
    return HttpResponse.json({
      sources: [
        {
          id: 1,
          providerName: 'Oura',
          isEnabled: true,
          lastSyncedAt: '2026-01-10T10:00:00Z',
        },
      ],
    });
  }),
];

// src/tests/setup.ts
import { setupServer } from 'msw/node';
import { handlers } from './mocks/handlers';

export const server = setupServer(...handlers);

// Start server before all tests
beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));

// Reset handlers after each test
afterEach(() => server.resetHandlers());

// Close server after all tests
afterAll(() => server.close());
```

---

## 4. E2E Testing with Playwright

### 4.1 Playwright for .NET API

#### 4.1.1 Configuration

```csharp
// Tests/E2E/PlaywrightFixture.cs
public class PlaywrightFixture : IAsyncLifetime
{
    public IPlaywright Playwright { get; private set; } = null!;
    public IBrowser Browser { get; private set; } = null!;
    public IAPIRequestContext ApiContext { get; private set; } = null!;

    private const string BaseUrl = "http://localhost:5000";

    public async Task InitializeAsync()
    {
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        Browser = await Playwright.Chromium.LaunchAsync(new()
        {
            Headless = true
        });

        ApiContext = await Playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = BaseUrl
        });
    }

    public async Task DisposeAsync()
    {
        await ApiContext.DisposeAsync();
        await Browser.DisposeAsync();
        Playwright.Dispose();
    }
}
```

#### 4.1.2 API E2E Tests

```csharp
// Tests/E2E/MetricsApiE2ETests.cs
public class MetricsApiE2ETests : IClassFixture<PlaywrightFixture>
{
    private readonly PlaywrightFixture _fixture;

    public MetricsApiE2ETests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetLatestMetrics_ReturnsValidResponse()
    {
        // Act
        var response = await _fixture.ApiContext.GetAsync("/api/metrics/latest");

        // Assert
        Assert.True(response.Ok);
        Assert.Equal(200, response.Status);

        var json = await response.JsonAsync();
        Assert.NotNull(json);

        // Verify response structure
        var metrics = json.Value.GetProperty("metrics");
        Assert.True(metrics.GetArrayLength() > 0);
    }

    [Fact]
    public async Task GetMetricsInRange_WithValidDates_ReturnsData()
    {
        // Arrange
        var from = DateTime.UtcNow.AddDays(-7).ToString("o");
        var to = DateTime.UtcNow.ToString("o");

        // Act
        var response = await _fixture.ApiContext.GetAsync(
            $"/api/metrics/range?from={from}&to={to}");

        // Assert
        Assert.True(response.Ok);
        var json = await response.JsonAsync();
        Assert.NotNull(json);
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthy()
    {
        // Act
        var response = await _fixture.ApiContext.GetAsync("/health");

        // Assert
        Assert.True(response.Ok);
        var body = await response.TextAsync();
        Assert.Contains("Healthy", body);
    }
}
```

### 4.2 Playwright for React SPA

#### 4.2.1 Configuration

```typescript
// playwright.config.ts
import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: [
    ['html'],
    ['junit', { outputFile: 'test-results/junit.xml' }],
  ],
  use: {
    baseURL: 'http://localhost:3000',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
    {
      name: 'firefox',
      use: { ...devices['Desktop Firefox'] },
    },
    {
      name: 'webkit',
      use: { ...devices['Desktop Safari'] },
    },
    {
      name: 'Mobile Chrome',
      use: { ...devices['Pixel 5'] },
    },
  ],
  webServer: {
    command: 'npm run dev',
    url: 'http://localhost:3000',
    reuseExistingServer: !process.env.CI,
  },
});
```

#### 4.2.2 UI E2E Tests

```typescript
// e2e/dashboard.spec.ts
import { test, expect } from '@playwright/test';

test.describe('Dashboard Page', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/dashboard');
  });

  test('displays dashboard heading', async ({ page }) => {
    await expect(page.getByRole('heading', { name: 'Health Dashboard' })).toBeVisible();
  });

  test('displays metric cards', async ({ page }) => {
    await expect(page.getByText('Sleep Score')).toBeVisible();
    await expect(page.getByText('Readiness')).toBeVisible();
    await expect(page.getByText('Activity Score')).toBeVisible();
    await expect(page.getByText('Steps')).toBeVisible();
  });

  test('loads data from API', async ({ page }) => {
    // Wait for API call
    await page.waitForResponse((response) =>
      response.url().includes('/api/dashboard/summary') && response.status() === 200
    );

    // Verify data is displayed (not placeholder)
    const sleepScore = page.locator('[data-testid="sleep-score-value"]');
    await expect(sleepScore).not.toHaveText('--');
  });

  test('handles API error gracefully', async ({ page }) => {
    // Mock API error
    await page.route('**/api/dashboard/summary', (route) =>
      route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({ error: 'Internal Server Error' }),
      })
    );

    await page.reload();

    // Verify error message is displayed
    await expect(page.getByText(/error/i)).toBeVisible();
  });

  test('refreshes data when button clicked', async ({ page }) => {
    const refreshButton = page.getByRole('button', { name: /refresh/i });
    await refreshButton.click();

    // Verify API is called again
    await page.waitForResponse((response) =>
      response.url().includes('/api/dashboard/summary')
    );
  });
});
```

#### 4.2.3 Navigation Tests

```typescript
// e2e/navigation.spec.ts
import { test, expect } from '@playwright/test';

test.describe('Navigation', () => {
  test('navigates between pages', async ({ page }) => {
    await page.goto('/');

    // Click on Metrics link
    await page.getByRole('link', { name: 'Metrics' }).click();
    await expect(page).toHaveURL('/metrics');
    await expect(page.getByRole('heading', { name: 'Metrics' })).toBeVisible();

    // Click on Sources link
    await page.getByRole('link', { name: 'Sources' }).click();
    await expect(page).toHaveURL('/sources');
    await expect(page.getByRole('heading', { name: 'Data Sources' })).toBeVisible();

    // Back to Dashboard
    await page.getByRole('link', { name: 'Dashboard' }).click();
    await expect(page).toHaveURL('/dashboard');
  });

  test('redirects root to dashboard', async ({ page }) => {
    await page.goto('/');
    await expect(page).toHaveURL('/dashboard');
  });
});
```

#### 4.2.4 Accessibility Tests

```typescript
// e2e/accessibility.spec.ts
import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

test.describe('Accessibility', () => {
  test('dashboard page should not have accessibility violations', async ({ page }) => {
    await page.goto('/dashboard');

    const accessibilityScanResults = await new AxeBuilder({ page }).analyze();

    expect(accessibilityScanResults.violations).toEqual([]);
  });

  test('supports keyboard navigation', async ({ page }) => {
    await page.goto('/dashboard');

    // Tab through interactive elements
    await page.keyboard.press('Tab');
    await expect(page.getByRole('link', { name: 'Dashboard' })).toBeFocused();

    await page.keyboard.press('Tab');
    await expect(page.getByRole('link', { name: 'Metrics' })).toBeFocused();
  });
});
```

---

## 5. Test Data Management

### 5.1 Test Data Builder Pattern

```csharp
// Tests/Builders/MeasurementBuilder.cs
public class MeasurementBuilder
{
    private int _metricTypeId = 1;
    private long _sourceId = 1;
    private decimal _value = 85;
    private DateTime _timestamp = DateTime.UtcNow;

    public MeasurementBuilder WithMetricType(int metricTypeId)
    {
        _metricTypeId = metricTypeId;
        return this;
    }

    public MeasurementBuilder WithSource(long sourceId)
    {
        _sourceId = sourceId;
        return this;
    }

    public MeasurementBuilder WithValue(decimal value)
    {
        _value = value;
        return this;
    }

    public MeasurementBuilder WithTimestamp(DateTime timestamp)
    {
        _timestamp = timestamp;
        return this;
    }

    public Measurement Build()
    {
        return new Measurement
        {
            MetricTypeId = _metricTypeId,
            SourceId = _sourceId,
            Value = _value,
            Timestamp = _timestamp,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}

// Usage
var measurement = new MeasurementBuilder()
    .WithMetricType(1)
    .WithValue(90)
    .WithTimestamp(DateTime.UtcNow.AddDays(-1))
    .Build();
```

### 5.2 Test Data Seeding

```csharp
// Tests/Data/TestDataSeeder.cs
public static class TestDataSeeder
{
    public static void SeedHealthData(HealthDbContext context)
    {
        // Metric Types
        var metricTypes = new[]
        {
            new MetricType { Id = 1, Name = "sleep_score", Unit = "score", Category = "Sleep" },
            new MetricType { Id = 2, Name = "steps", Unit = "steps", Category = "Activity" },
            new MetricType { Id = 3, Name = "weight", Unit = "kg", Category = "Body" }
        };
        context.MetricTypes.AddRange(metricTypes);

        // Sources
        var sources = new[]
        {
            new Source { Id = 1, ProviderName = "Oura", IsEnabled = true },
            new Source { Id = 2, ProviderName = "Picooc", IsEnabled = true }
        };
        context.Sources.AddRange(sources);

        // Measurements
        var measurements = new[]
        {
            new MeasurementBuilder().WithMetricType(1).WithSource(1).WithValue(85).Build(),
            new MeasurementBuilder().WithMetricType(2).WithSource(1).WithValue(10000).Build(),
            new MeasurementBuilder().WithMetricType(3).WithSource(2).WithValue(75.5m).Build()
        };
        context.Measurements.AddRange(measurements);

        context.SaveChanges();
    }
}
```

### 5.3 Test Isolation and Cleanup

```csharp
// Tests/Integration/DatabaseFixture.cs
public class DatabaseFixture : IDisposable
{
    public HealthDbContext Context { get; }

    public DatabaseFixture()
    {
        var options = new DbContextOptionsBuilder<HealthDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        Context = new HealthDbContext(options);
        TestDataSeeder.SeedHealthData(Context);
    }

    public void Dispose()
    {
        Context.Database.EnsureDeleted();
        Context.Dispose();
    }
}

// Collection definition for shared context
[CollectionDefinition("Database collection")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}

// Usage in tests
[Collection("Database collection")]
public class MyIntegrationTests
{
    private readonly DatabaseFixture _fixture;

    public MyIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }
}
```

---

## 6. CI/CD Integration

### 6.1 GitHub Actions Workflow

```yaml
# .github/workflows/test.yml
name: Test Suite

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  unit-tests:
    name: Unit Tests
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Restore dependencies
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore

      - name: Run unit tests
        run: dotnet test --no-build --verbosity normal --collect:"XPlat Code Coverage" --results-directory ./coverage

      - name: Upload coverage
        uses: codecov/codecov-action@v3
        with:
          files: ./coverage/**/coverage.cobertura.xml

  integration-tests:
    name: Integration Tests
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Run integration tests
        run: dotnet test Tests/Integration --filter Category=Integration

  e2e-tests-api:
    name: E2E Tests - API
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Install Playwright
        run: pwsh Tests/E2E/bin/Debug/net8.0/playwright.ps1 install

      - name: Run E2E tests
        run: dotnet test Tests/E2E --filter Category=E2E

  e2e-tests-ui:
    name: E2E Tests - UI
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      - name: Setup Node
        uses: actions/setup-node@v4
        with:
          node-version: '20'

      - name: Install dependencies
        run: npm ci

      - name: Install Playwright browsers
        run: npx playwright install --with-deps

      - name: Run Playwright tests
        run: npx playwright test

      - name: Upload test results
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: playwright-report
          path: playwright-report/

  react-tests:
    name: React Tests
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      - name: Setup Node
        uses: actions/setup-node@v4
        with:
          node-version: '20'

      - name: Install dependencies
        run: npm ci

      - name: Run Vitest tests
        run: npm run test:coverage

      - name: Upload coverage
        uses: codecov/codecov-action@v3
        with:
          files: ./coverage/coverage-final.json
```

---

## 7. Performance Testing Strategy

### 7.1 BenchmarkDotNet for .NET

```csharp
// Tests/Performance/RepositoryBenchmarks.cs
[MemoryDiagnoser]
public class RepositoryBenchmarks
{
    private HealthDbContext _context = null!;
    private MeasurementsRepository _repository = null!;

    [GlobalSetup]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<HealthDbContext>()
            .UseInMemoryDatabase("BenchmarkDb")
            .Options;

        _context = new HealthDbContext(options);
        _repository = new MeasurementsRepository(_context, Mock.Of<ILogger<MeasurementsRepository>>());

        // Seed large dataset
        var measurements = Enumerable.Range(1, 10000)
            .Select(i => new Measurement
            {
                MetricTypeId = 1,
                SourceId = 1,
                Value = i,
                Timestamp = DateTime.UtcNow.AddDays(-i)
            });

        _context.Measurements.AddRange(measurements);
        _context.SaveChanges();
    }

    [Benchmark]
    public async Task GetLatestByMetricTypeAsync()
    {
        await _repository.GetLatestByMetricTypeAsync("sleep_score");
    }

    [Benchmark]
    public async Task GetByMetricTypeInRangeAsync()
    {
        var from = DateTime.UtcNow.AddDays(-30);
        var to = DateTime.UtcNow;
        await _repository.GetByMetricTypeInRangeAsync("sleep_score", from, to);
    }
}
```

### 7.2 Lighthouse CI for React SPA

```javascript
// lighthouserc.js
module.exports = {
  ci: {
    collect: {
      url: ['http://localhost:3000/dashboard', 'http://localhost:3000/metrics'],
      numberOfRuns: 3,
    },
    assert: {
      assertions: {
        'categories:performance': ['warn', { minScore: 0.9 }],
        'categories:accessibility': ['error', { minScore: 0.95 }],
        'categories:best-practices': ['warn', { minScore: 0.9 }],
        'categories:seo': ['warn', { minScore: 0.9 }],
      },
    },
    upload: {
      target: 'temporary-public-storage',
    },
  },
};
```

---

## 8. Test Coverage Goals

### 8.1 Coverage Targets

- **Overall Code Coverage**: 80%+
- **Critical Paths**: 95%+
- **Service Layer**: 85%+
- **Repository Layer**: 90%+
- **API Endpoints**: 100%
- **React Components**: 80%+
- **Custom Hooks**: 90%+

### 8.2 Coverage Configuration

```xml
<!-- coverlet.runsettings -->
<?xml version="1.0" encoding="utf-8" ?>
<RunSettings>
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName="XPlat code coverage">
        <Configuration>
          <Format>cobertura,opencover</Format>
          <Exclude>[*.Tests]*,[*.E2E]*</Exclude>
          <Include>[HealthAggregatorV2.*]*</Include>
          <ExcludeByAttribute>Obsolete,GeneratedCode,CompilerGenerated</ExcludeByAttribute>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
</RunSettings>
```

---

## 9. Gray Areas / Questions

### 9.1 Production-Like Test Environment

**Question:** Should tests run against a staging environment with real Azure SQL?

**Options:**
- **Option 1:** In-memory database only (fast, isolated)
- **Option 2:** LocalDB for integration tests (more realistic)
- **Option 3:** Azure SQL staging database (production-like)

**Recommendation:** Option 1 for unit tests, Option 2 for integration tests, Option 3 for smoke tests pre-production.

### 9.2 Test Data Privacy

**Decision:** Use synthetic test data only. No privacy requirements or concerns for test data.

### 9.3 Load Testing Scope

**Question:** Should load testing be performed on Free tier resources?

**Options:**
- **Option 1:** No load testing (free tier has limits)
- **Option 2:** Lightweight load testing with k6
- **Option 3:** Full load testing on separate paid tier

**Recommendation:** Option 1 initially, upgrade to Option 2 when scaling.

### 9.4 Flaky Test Handling

**Question:** How should flaky tests be handled?

**Options:**
- **Option 1:** Retry failed tests automatically
- **Option 2:** Quarantine flaky tests
- **Option 3:** Fix root cause immediately

**Recommendation:** Option 3 (fix immediately), with Option 1 as temporary mitigation.

### 9.5 Test Parallelization

**Question:** Should tests run in parallel?

**Options:**
- **Option 1:** Sequential execution (safer, slower)
- **Option 2:** Parallel execution with isolated databases
- **Option 3:** Parallel with shared database and transactions

**Recommendation:** Option 2 (parallel with isolation) for speed.

---

## 10. Implementation Checklist

- [ ] Set up xUnit for .NET unit tests
- [ ] Set up Vitest for React unit tests
- [ ] Configure in-memory database for repository tests
- [ ] Create test data builders and seeders
- [ ] Write unit tests for services and repositories
- [ ] Write component tests for React UI
- [ ] Set up MSW for API mocking in React tests
- [ ] Configure WebApplicationFactory for integration tests
- [ ] Write integration tests for API endpoints
- [ ] Install Playwright for .NET and TypeScript
- [ ] Write E2E tests for API with Playwright
- [ ] Write E2E tests for UI with Playwright
- [ ] Add accessibility tests with axe-core
- [ ] Configure test coverage reporting
- [ ] Set up CI/CD pipeline for automated testing
- [ ] Add BenchmarkDotNet for performance tests
- [ ] Configure Lighthouse CI for performance monitoring
- [ ] Document testing best practices
- [ ] Achieve 80%+ code coverage
- [ ] Monitor and fix flaky tests

---

## 11. References

### Microsoft Documentation

- [xUnit Testing Best Practices](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)
- [Integration Tests in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests)
- [EF Core Testing](https://learn.microsoft.com/en-us/ef/core/testing/)
- [Playwright for .NET](https://playwright.dev/dotnet/)

### Testing Libraries

- [Playwright Documentation](https://playwright.dev/)
- [Vitest Documentation](https://vitest.dev/)
- [MSW (Mock Service Worker)](https://mswjs.io/)
- [BenchmarkDotNet](https://benchmarkdotnet.org/)

### Best Practices

- Write tests first (TDD) for complex logic
- Use test data builders for maintainability
- Mock external dependencies
- Test behavior, not implementation
- Aim for fast, isolated, deterministic tests
- Use in-memory databases for unit tests
- Test error paths, not just happy paths
- Run tests in CI/CD pipeline
- Monitor test coverage trends
- Fix flaky tests immediately
