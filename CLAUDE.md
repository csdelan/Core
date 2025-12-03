# CLAUDE.md - AI Assistant Guide for Core Repository

**Last Updated:** December 3, 2025
**Repository:** Core - .NET 8.0 Utility Library
**Code Coverage:** 71.2%

## Table of Contents
1. [Project Overview](#project-overview)
2. [Repository Structure](#repository-structure)
3. [Key Architectural Patterns](#key-architectural-patterns)
4. [Development Workflows](#development-workflows)
5. [Coding Conventions](#coding-conventions)
6. [Testing Standards](#testing-standards)
7. [Common Tasks Guide](#common-tasks-guide)
8. [Important Files Reference](#important-files-reference)
9. [AI Assistant Guidelines](#ai-assistant-guidelines)

---

## Project Overview

The **Core** repository is a well-maintained .NET 8.0 utility library providing foundational classes and patterns for enterprise applications. It demonstrates professional development practices with comprehensive documentation, extensive testing, and clear architectural patterns.

### Technology Stack
- **.NET 8.0** - Target framework
- **xUnit 2.5.3** - Unit testing framework
- **Serilog 4.0.1** - Structured logging
- **GitVersion.MsBuild 6.5.0** - Automatic semantic versioning
- **Google Sheets API v4** - External service integration (GoogleSheets project)
- **Windows MediaPlayer COM** - Audio playback (Audio project)

### Project Maturity
- **Test Coverage:** 71.2%
- **Documentation:** Comprehensive XML comments on core classes
- **Git Workflow:** Pull request-based with AI-assisted development (claude/ and copilot/ branch prefixes)
- **Build System:** Clean .NET build with GitVersion for continuous deployment

---

## Repository Structure

```
/home/user/Core/
├── Core/                          # Main library (1836 LOC)
│   ├── ValueObject.cs            # DDD value object base (272 lines) ⭐
│   ├── BaseEvent.cs              # Event tracking system (30 lines)
│   ├── PersonalFile.cs           # File metadata & hashing (72 lines)
│   ├── PersonalFileDb.cs         # File repository manager (96 lines)
│   ├── TagList.cs                # Tag collection (45 lines)
│   ├── TagCloud.cs               # Tag analysis (58 lines)
│   ├── Env.cs                    # Environment config (65 lines)
│   ├── DateTimeOffsetExtensions.cs # Fluent date extensions (31 lines)
│   ├── ComputerInfo.cs           # Windows system info (38 lines)
│   ├── FileDatabase.cs           # Stub for future implementation (30 lines)
│   ├── Core.csproj
│   └── Core.sln
│
├── Core.Tests/                   # xUnit test suite
│   ├── ValueObjectTest.cs        # 16 comprehensive tests (250 lines)
│   ├── BaseEventTest.cs          # 5 test methods (110 lines)
│   ├── AppEnvTest.cs             # 10 environment tests (235 lines)
│   ├── PersonalFileTest.cs       # File hashing tests
│   ├── TagsTest.cs               # Tag collection tests
│   ├── TagCloudTest.cs           # Tag statistics tests (86 lines)
│   ├── DateTimeOffsetExtensionsTest.cs # Extension method tests (78 lines)
│   └── Core.Tests.csproj
│
├── Core.GoogleSheets/            # Google Sheets integration
│   ├── GoogleWorksheet.cs        # Sheet operations (98 lines)
│   ├── RowTable.cs               # Strongly-typed CRUD (19K lines)
│   ├── SheetColumnAttribute.cs   # Column mapping metadata
│   ├── SheetKeyAttribute.cs      # Key column metadata
│   └── Core.GoogleSheets.csproj
│
├── Core.Audio/                   # Audio playback (Windows-only)
│   ├── AudioManager.cs           # Queue-based playback (74 lines)
│   ├── AudioTrack.cs             # MediaPlayer wrapper (63 lines)
│   └── Core.Audio.csproj
│
├── GitVersion.yml                # Continuous deployment versioning
└── .gitignore                    # Standard .NET ignore patterns
```

### ⭐ Core Classes to Understand First

1. **ValueObject.cs** - Abstract base for DDD value objects with structural equality
2. **BaseEvent.cs** - Event tracking with workflow states
3. **PersonalFile.cs** - File metadata with SHA256 hashing
4. **TagList.cs** + **TagCloud.cs** - Tag management system
5. **Env.cs (App static class)** - Environment and secret management

---

## Key Architectural Patterns

### 1. Value Object Pattern (ValueObject.cs)

**Purpose:** Implements Domain-Driven Design value objects with structural equality (not identity-based).

**Key Features:**
- Abstract base class requiring `GetEqualityComponents()` implementation
- Cached hash codes using polynomial rolling hash (factor 23) for performance
- Handles ORM proxies transparently (Entity Framework Core, NHibernate)
- Implements `IComparable<ValueObject>` for ordering
- Null-safe operator overloads (==, !=)

**Example Usage:**
```csharp
public class Address : ValueObject
{
    public string Street { get; }
    public string City { get; }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
    }
}
```

**Important:** Two value objects are equal if all their equality components are equal, regardless of object identity.

### 2. Repository Pattern (PersonalFileDb.cs)

**Purpose:** Manages collections with background processing capabilities.

**Key Features:**
- HashSet-based storage for deduplication
- Background worker for async rebuilding
- Real-time file watcher support
- Serilog integration for operation logging

### 3. Event-Driven Workflow (BaseEvent.cs)

**Purpose:** Track events through defined lifecycle states.

**Workflow States:**
```
Unread → Read → Processing → Completed
```

**Properties:**
- Auto-managed timestamps (DateCreated, DateModified, DateClosed)
- Work tracking (StartedWorkDateTime, CompletedWorkDateTime)
- Priority and persistence flags
- URL and Payload for external references

### 4. Attribute-Based Mapping (GoogleSheets)

**Purpose:** ORM-like strongly-typed operations for Google Sheets.

**Key Components:**
- `[SheetColumn]` - Maps properties to columns
- `[SheetKey]` - Marks key column for upsert operations
- `RowTable<T>` - Generic CRUD operations with type safety
- Automatic header validation and repair

### 5. Environment Configuration (Env.cs)

**Purpose:** Multi-environment secret and config management.

**Supported Environments:**
- `Dev` (default if RUNTIME_ENVIRONMENT not set)
- `Staging`
- `Prod`

**Methods:**
- `App.GetSecret(name)` → Retrieves `{ENV}_{name}` environment variable
- `App.GetGlobalSecret(name)` → Retrieves global secrets
- `App.GetConfigFilename(name)` → Returns `config.{env}.json`

### 6. Extension Method Pattern (DateTimeOffsetExtensions.cs)

**Purpose:** Fluent API for DateTimeOffset mutations.

**Example:**
```csharp
var newDate = existingDate.WithDay(15).WithDayAndMonth(3, 15);
var truncated = timestamp.TruncateToMinute();
```

---

## Development Workflows

### Git Workflow

**Branch Naming Convention:**
- `claude/*` - AI-assisted development branches (Claude Code)
- `copilot/*` - AI-assisted development branches (GitHub Copilot)
- Feature branches follow: `<tool>/<description>-<session-id>`

**Example:** `claude/add-xml-comments-valueobject-019LiGue87UbnVPMkiuVDHhq`

**Commit Standards:**
- Clear, descriptive commit messages
- Reference PR numbers when merging
- Incremental commits showing work progression
- Example: "Add comprehensive XML documentation comments to ValueObject class"

**Pull Request Process:**
1. Create feature branch with appropriate prefix
2. Make incremental commits with clear messages
3. Ensure all tests pass (71.2%+ coverage)
4. Create PR with descriptive title
5. Merge to main branch after review

### Build Process

**Standard Build:**
```bash
dotnet build Core/Core.sln
```

**Run Tests:**
```bash
dotnet test Core.Tests/Core.Tests.csproj
```

**With Coverage:**
```bash
dotnet test Core.Tests/Core.Tests.csproj --collect:"XPlat Code Coverage"
```

**GitVersion Integration:**
- Automatically increments version based on commits
- Mode: ContinuousDeployment
- Tag prefix: `v`
- Semantic version format: Loose

### Testing Workflow

**Requirements:**
- All new code must have corresponding tests
- Maintain or improve 71.2% coverage threshold
- Follow AAA pattern (Arrange-Act-Assert)
- Use descriptive test names: `[Method]_[Condition]_[Expected]`

**Example Test Structure:**
```csharp
[Fact]
public void Equals_WithSameValues_ShouldReturnTrue()
{
    // Arrange
    var obj1 = new TestValueObject("test", 42);
    var obj2 = new TestValueObject("test", 42);

    // Act & Assert
    Assert.True(obj1.Equals(obj2));
}
```

---

## Coding Conventions

### Naming Standards
- **Classes/Methods/Properties:** PascalCase (standard C#)
- **File Names:** Match primary class name exactly
- **Test Classes:** `[ClassName]Test` or `[ClassName]Tests`
- **Private Fields:** `_camelCase` with underscore prefix

### Code Style
- **Documentation:** Comprehensive XML comments on all public APIs
- **Immutability:** Prefer immutable objects with private/init setters
- **Date/Time:** Use `DateTimeOffset` (not `DateTime`) for timezone awareness
- **Null Safety:** Nullable reference types enabled globally (`<Nullable>enable</Nullable>`)
- **Property Defaults:** Initialize reference type properties with empty strings
- **Required Properties:** Use `required` keyword (.NET 7+)

### Example Class Structure
```csharp
namespace Core
{
    /// <summary>
    /// Comprehensive XML documentation here.
    /// </summary>
    /// <remarks>
    /// Additional details and usage examples.
    /// </remarks>
    public class ExampleClass : ValueObject
    {
        private int? _cachedValue;

        /// <summary>
        /// Property documentation.
        /// </summary>
        public string Name { get; init; } = string.Empty;

        public required int Value { get; init; }

        /// <summary>
        /// Method documentation.
        /// </summary>
        /// <param name="parameter">Parameter description.</param>
        /// <returns>Return value description.</returns>
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Name;
            yield return Value;
        }
    }
}
```

### Error Handling
- **Null Checks:** Explicit with `ArgumentNullException`
- **Custom Messages:** Always provide descriptive error messages
- **Graceful Fallbacks:** Default values when appropriate (e.g., `App.Env` defaults to `Dev`)
- **Resilience:** Try-catch in properties when dealing with external resources

### Async Patterns
- **Async Suffix:** Methods returning `Task` should end with `Async`
- **CancellationToken:** Accept as last parameter for all async methods
- **ConfigureAwait:** Not used (modern .NET 8.0 practice)
- **Example:** `GetCellAsync(string a1, CancellationToken cancellationToken = default)`

---

## Testing Standards

### Test Framework
- **xUnit 2.5.3** - Modern testing framework
- **coverlet.collector 6.0.0** - Code coverage measurement
- **No setup/teardown attributes** - Use constructor/IDisposable

### Test Organization
- **One test class per production class**
- **Test class in same namespace with .Tests suffix**
- **Group related tests with clear naming**

### Test Naming Convention
```
[MethodName]_[Condition]_[ExpectedResult]
```

**Examples:**
- `Equals_WithSameValues_ShouldReturnTrue`
- `GetHashCode_WithSameValues_ShouldReturnSameHash`
- `CompareTo_WithNull_ShouldReturnOne`
- `GetSecret_WithDevEnvironment_ShouldReturnDevSecret`

### Test Patterns

**1. Equality Testing (ValueObject):**
```csharp
[Fact]
public void Equals_WithSameValues_ShouldReturnTrue()
{
    // Arrange
    var obj1 = new TestValueObject("test", 42);
    var obj2 = new TestValueObject("test", 42);

    // Act & Assert
    Assert.True(obj1.Equals(obj2));
    Assert.True(obj2.Equals(obj1)); // Symmetry
}
```

**2. Exception Testing:**
```csharp
[Fact]
public void Constructor_WithNullItems_ShouldThrowArgumentNullException()
{
    // Act & Assert
    Assert.Throws<ArgumentNullException>(() => new TagCloud(null));
}
```

**3. Property Initialization Testing:**
```csharp
[Fact]
public void DefaultConstructor_ShouldInitializeProperties()
{
    // Arrange & Act
    var evt = new BaseEvent();

    // Assert
    Assert.Equal(EventStatus.Unread, evt.Status);
    Assert.NotEqual(DateTimeOffset.MinValue, evt.DateCreated);
}
```

**4. File I/O Testing (with cleanup):**
```csharp
public class PersonalFileTest : IDisposable
{
    private readonly string _tempDir;

    public PersonalFileTest()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }
}
```

### Code Coverage Goals
- **Current:** 71.2%
- **Target:** Maintain or improve
- **Focus:** Core logic and edge cases
- **Skip:** Platform-specific code (e.g., `ComputerInfo.cs` on non-Windows)

---

## Common Tasks Guide

### Adding a New Value Object

1. **Create class inheriting from `ValueObject`:**
```csharp
public class YourValueObject : ValueObject
{
    public string Property1 { get; init; } = string.Empty;
    public int Property2 { get; init; }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Property1;
        yield return Property2;
    }
}
```

2. **Create corresponding test class:**
```csharp
public class YourValueObjectTest
{
    [Fact]
    public void Equals_WithSameValues_ShouldReturnTrue()
    {
        // Test equality
    }

    [Fact]
    public void GetHashCode_WithSameValues_ShouldReturnSameHash()
    {
        // Test hash code consistency
    }
}
```

3. **Build and test:**
```bash
dotnet build
dotnet test
```

### Adding Environment Configuration

1. **Set environment variable:**
```bash
export RUNTIME_ENVIRONMENT=Prod
export PROD_DATABASE_URL="your-connection-string"
```

2. **Retrieve in code:**
```csharp
var dbUrl = App.GetSecret("DATABASE_URL"); // Gets PROD_DATABASE_URL
```

3. **Create config file:**
```bash
config.prod.json
config.dev.json
config.staging.json
```

4. **Load config file:**
```csharp
var configPath = App.GetConfigFilename("settings"); // Returns "config.prod.json"
```

### Adding Google Sheets Integration

1. **Define model with attributes:**
```csharp
public class YourModel
{
    [SheetKey]
    [SheetColumn("ID")]
    public string Id { get; set; }

    [SheetColumn("Name")]
    public string Name { get; set; }

    [SheetColumn("Value", Index = 2)]
    public int Value { get; set; }
}
```

2. **Create RowTable instance:**
```csharp
var service = new SheetsService(/* credentials */);
var worksheet = new GoogleWorksheet(service, "spreadsheetId", "SheetName");
var table = new RowTable<YourModel>(worksheet);
```

3. **Perform CRUD operations:**
```csharp
await table.UpsertAsync(yourModel); // Insert or update by key
var allRows = await table.GetAllAsync();
```

### Running Tests with Coverage

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test class
dotnet test --filter "FullyQualifiedName~ValueObjectTest"

# Run tests matching pattern
dotnet test --filter "Name~Equals"
```

### Creating a Pull Request

1. **Ensure on feature branch:**
```bash
git checkout -b claude/your-feature-description-sessionid
```

2. **Make changes and commit:**
```bash
git add .
git commit -m "Clear description of changes"
```

3. **Push to remote:**
```bash
git push -u origin claude/your-feature-description-sessionid
```

4. **Verify tests pass:**
```bash
dotnet test
```

5. **Create PR via GitHub UI or gh CLI**

---

## Important Files Reference

### Must-Read Files for AI Assistants

| File | Lines | Priority | Purpose |
|------|-------|----------|---------|
| `ValueObject.cs` | 272 | ⭐⭐⭐ | Core DDD pattern with extensive docs |
| `BaseEvent.cs` | 30 | ⭐⭐ | Event tracking workflow |
| `Env.cs` | 65 | ⭐⭐ | Environment configuration |
| `PersonalFile.cs` | 72 | ⭐⭐ | File metadata and hashing |
| `ValueObjectTest.cs` | 250 | ⭐⭐⭐ | Reference test patterns |
| `Core.csproj` | ~30 | ⭐ | Build configuration |
| `GitVersion.yml` | ~15 | ⭐ | Versioning strategy |

### Configuration Files

- **Core.csproj** - Main project dependencies and settings
- **Core.Tests.csproj** - Test project configuration
- **GitVersion.yml** - Semantic versioning configuration
- **.gitignore** - Standard .NET ignore patterns

### Documentation References

**External Resources:**
- ValueObject pattern: https://enterprisecraftsmanship.com/posts/value-object-better-implementation/
- DDD concepts: https://martinfowler.com/bliki/DomainDrivenDesign.html
- xUnit documentation: https://xunit.net/docs/getting-started/netcore/cmdline

---

## AI Assistant Guidelines

### When Working on This Repository

#### DO:
✅ **Read files before modifying** - Always use Read tool before Edit tool
✅ **Follow existing patterns** - Study similar classes before adding new ones
✅ **Write tests first** - TDD approach is preferred
✅ **Add XML documentation** - All public APIs must have comprehensive docs
✅ **Use ValueObject base** - For domain objects with structural equality
✅ **Prefer DateTimeOffset** - Over DateTime for timezone awareness
✅ **Check test coverage** - Maintain 71.2%+ coverage
✅ **Follow naming conventions** - PascalCase, descriptive test names
✅ **Use Serilog for logging** - Structured logging throughout
✅ **Handle nulls explicitly** - Throw ArgumentNullException with clear messages
✅ **Make incremental commits** - Small, focused commits with clear messages
✅ **Use async/await** - For all I/O operations

#### DON'T:
❌ **Create files without reading existing** - Understand context first
❌ **Skip XML documentation** - Required for all public APIs
❌ **Use DateTime** - Use DateTimeOffset instead
❌ **Break existing tests** - All tests must pass
❌ **Reduce code coverage** - Must maintain or improve
❌ **Add dependencies casually** - Discuss major dependency additions
❌ **Ignore ORM proxy handling** - ValueObject handles this; preserve it
❌ **Skip async suffix** - Methods returning Task must end with Async
❌ **Forget IDisposable in tests** - Clean up test resources
❌ **Make breaking changes** - This is a library; API stability matters

### Understanding the Codebase

**Start Here:**
1. Read `ValueObject.cs` - Understanding this is critical
2. Read `ValueObjectTest.cs` - See how to test properly
3. Read `BaseEvent.cs` - Simple, well-structured example
4. Scan `Core.csproj` - Know the dependencies
5. Review recent commits - Understand development patterns

**Common Questions:**

**Q: How do I create a new domain object?**
A: Inherit from `ValueObject` and implement `GetEqualityComponents()`. See `PersonalFile.cs` for example.

**Q: How do I add environment-specific configuration?**
A: Use `App.GetSecret("KEY_NAME")` which automatically prefixes with environment (DEV_, STAGING_, PROD_).

**Q: How do I write tests?**
A: Follow AAA pattern, use descriptive names like `Method_Condition_Expected`, see `ValueObjectTest.cs`.

**Q: How do I handle file operations?**
A: See `PersonalFile.cs` for hashing and metadata, `PersonalFileDb.cs` for repository pattern.

**Q: How do I work with Google Sheets?**
A: Use `[SheetColumn]` and `[SheetKey]` attributes with `RowTable<T>`. See `Core.GoogleSheets/` project.

**Q: What's the branching strategy?**
A: Create branch with `claude/` or `copilot/` prefix, make changes, create PR to main.

**Q: How do I check test coverage?**
A: Run `dotnet test --collect:"XPlat Code Coverage"`. Current threshold is 71.2%.

### Common Code Patterns

**Pattern 1: Value Object with Equality**
```csharp
public class Money : ValueObject
{
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
```

**Pattern 2: Event with Workflow**
```csharp
var evt = new BaseEvent
{
    Name = "ProcessOrder",
    Status = EventStatus.Unread,
    Priority = 1
};
evt.Status = EventStatus.Processing;
evt.StartedWorkDateTime = DateTimeOffset.UtcNow;
```

**Pattern 3: Tag Management**
```csharp
var tags = new TagList("urgent important customer");
var file = new PersonalFile(path) { Tags = tags };
var cloud = new TagCloud(files);
var stats = cloud.GetTagStatistics(); // Dictionary<string, int>
```

**Pattern 4: Environment Config**
```csharp
// Set: export DEV_API_KEY="dev-key-123"
// Set: export PROD_API_KEY="prod-key-456"
var apiKey = App.GetSecret("API_KEY"); // Returns appropriate key
var configPath = App.GetConfigFilename("app"); // Returns config.dev.json or config.prod.json
```

### Testing Checklist

Before committing, verify:
- [ ] All tests pass: `dotnet test`
- [ ] Code coverage maintained: `dotnet test --collect:"XPlat Code Coverage"`
- [ ] XML documentation added for public APIs
- [ ] Null checks added with ArgumentNullException
- [ ] Async methods have Async suffix
- [ ] Test names follow `Method_Condition_Expected` pattern
- [ ] AAA pattern used in tests
- [ ] IDisposable implemented for test cleanup if needed
- [ ] No platform-specific code without guards
- [ ] ValueObject equality components properly implemented

### Debugging Tips

**Build Issues:**
```bash
dotnet clean
dotnet restore
dotnet build
```

**Test Issues:**
```bash
dotnet test --logger "console;verbosity=detailed"
dotnet test --filter "FullyQualifiedName~YourTestName"
```

**Coverage Issues:**
```bash
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage
```

**Git Issues:**
```bash
git status
git log --oneline -10
git diff HEAD
```

---

## Project Philosophy

This repository follows these principles:

1. **Domain-Driven Design** - ValueObject pattern is central to domain modeling
2. **Test-Driven Development** - 71.2% coverage with comprehensive tests
3. **Immutability** - Value objects are immutable by design
4. **Explicit is Better** - No magic; clear, understandable code
5. **Documentation First** - XML comments explain intent and usage
6. **Async-First** - All I/O operations are async
7. **Environment Awareness** - Multi-environment support built-in
8. **Separation of Concerns** - Core library separate from extensions (GoogleSheets, Audio)
9. **Professional Standards** - Clean commits, PR workflow, semantic versioning

---

## Changelog

| Date | Change | Commit |
|------|--------|--------|
| 2025-12-03 | Added XML documentation to ValueObject | 1813536 |
| 2025-12-03 | Improved test coverage to 71.2% | f49d10a |
| 2025-12-03 | Initial CLAUDE.md created | (current) |

---

## Questions or Issues?

When encountering issues:
1. Check existing tests for similar scenarios
2. Review ValueObject.cs for pattern examples
3. Check git history: `git log --all --grep="keyword"`
4. Verify environment variables are set correctly
5. Ensure .NET 8.0 SDK is installed

This guide is maintained for AI assistants working with this codebase. Keep it updated as the project evolves.
