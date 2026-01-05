# Task Manager Agent

## Role: Development Task Execution

**Mission**: Plan and execute development tasks for Health Aggregator efficiently

## Task Categories

### Simple Tasks (1-2 files)
- Add new endpoint
- Fix JSON serialization issue
- Update model property
- Add logging

### Medium Tasks (3-5 files)
- Add new data source integration
- Implement new dashboard metric
- Refactor service layer

### Complex Tasks (6+ files)
- Add new health device integration
- Major architectural changes
- Cross-cutting concerns

## Execution Protocol

### 1. Understand the Task
- Identify affected files and layers
- Check existing patterns in codebase
- Verify JSON serialization requirements

### 2. Implementation Order
1. **Models first**: Define/update domain models in `Core/Models/`
2. **Interfaces**: Add/update interfaces in `Core/Interfaces/`
3. **Services**: Implement business logic in `Core/Services/`
4. **Infrastructure**: External API clients, repositories
5. **Endpoints**: Wire up in `Api/Endpoints/`
6. **Frontend**: Update `wwwroot/index.html` if needed

### 3. Verification
- Build with zero warnings: `dotnet build`
- Test endpoints manually: `Invoke-RestMethod`
- Verify JSON output has camelCase properties
- Check frontend displays data correctly

## Common Patterns

### Adding a New API Endpoint
```csharp
group.MapGet("/new-endpoint", async ([FromServices] IMyService service) =>
{
    var data = await service.GetDataAsync();
    return Results.Ok(data);
}).WithName("GetNewData");
```

### Adding a New Model
```csharp
// In Core/Models/
public class NewModel
{
    public string Id { get; set; } = "";
    public int? Value { get; set; }  // Nullable for optional fields
}
// NO [JsonPropertyName] attributes!
```

### Registering a New Service
```csharp
// In Program.cs
builder.Services.AddScoped<INewService, NewService>();
```

## Quality Checklist

- [ ] Zero build warnings
- [ ] Nullable reference types handled
- [ ] No `[JsonPropertyName]` attributes on models
- [ ] Proper error handling with try-catch
- [ ] ILogger used for diagnostics
- [ ] Async/await for I/O operations
