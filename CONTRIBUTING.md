# Contributing to StashPlayaVR API

Thank you for your interest in contributing to StashPlayaVR API! This document provides guidelines and information for contributors.

## 🚀 Getting Started

### Prerequisites

- .NET 8.0 SDK
- Git
- Docker (optional, for testing)
- A running StashApp instance for testing

### Development Setup

1. **Fork and Clone**
   ```bash
   git clone https://github.com/yourusername/stashplayavr-api.git
   cd stashplayavr-api
   ```

2. **Configure Development Environment**
   ```bash
   cp src/appsettings.example.json src/appsettings.json
   # Edit src/appsettings.json with your StashApp configuration
   ```

3. **Restore Dependencies**
   ```bash
   dotnet restore src
   ```

4. **Run the Application**
   ```bash
   dotnet run --project src
   ```

## 📝 Development Guidelines

### Code Style

- Follow C# naming conventions
- Use meaningful variable and method names
- Add XML documentation for public APIs
- Keep methods focused and single-purpose
- Use async/await for I/O operations

### Project Structure

```
src/
├── Controllers/          # API controllers
│   ├── VideosController.cs
│   ├── ActorsController.cs
│   ├── StudiosController.cs
│   └── StreamController.cs
├── Services/            # Business logic services
│   ├── IAuthenticationService.cs
│   ├── AuthenticationService.cs
│   └── BackgroundDataSyncService.cs
├── Repositories/        # Data access layer
│   ├── IVideosRepository.cs
│   ├── VideosRepository.cs
│   └── ...
├── Filters/            # Action filters
│   ├── AuthenticationFilter.cs
│   └── ExceptionFilter.cs
├── Models/             # Data models
│   ├── Video.cs
│   ├── Actor.cs
│   └── ...
└── Program.cs          # Application entry point
```

### Testing

- Write unit tests for new functionality
- Test both success and failure scenarios
- Use meaningful test names that describe the behavior
- Mock external dependencies (StashApp API calls)

```bash
# Run tests
dotnet test src

# Run tests with coverage
dotnet test src --collect:"XPlat Code Coverage"
```

## 🐛 Bug Reports

When reporting bugs, please include:

1. **Environment Information**
   - OS and version
   - .NET version
   - Docker version (if applicable)

2. **Steps to Reproduce**
   - Clear, numbered steps
   - Expected vs actual behavior

3. **Configuration**
   - Relevant parts of `appsettings.json` (remove sensitive data)
   - Environment variables used

4. **Logs**
   - Application logs
   - Any error messages
   - Network logs if relevant

## ✨ Feature Requests

When requesting features:

1. **Describe the Problem**
   - What problem does this solve?
   - Who would benefit from this feature?

2. **Propose a Solution**
   - How should this work?
   - Any specific requirements or constraints?

3. **Additional Context**
   - Screenshots or mockups
   - Related issues or discussions
   - Alternative solutions considered

## 🔄 Pull Request Process

### Before Submitting

1. **Create a Branch**
   ```bash
   git checkout -b feature/your-feature-name
   # or
   git checkout -b fix/your-bug-fix
   ```

2. **Make Your Changes**
   - Write clean, well-documented code
   - Add tests for new functionality
   - Update documentation if needed

3. **Test Your Changes**
   ```bash
   dotnet build src
   dotnet test src
   ```

4. **Commit Your Changes**
   ```bash
   git add .
   git commit -m "Add: brief description of changes"
   ```

### Pull Request Guidelines

1. **Title and Description**
   - Use clear, descriptive titles
   - Provide detailed descriptions
   - Reference related issues

2. **Code Quality**
   - Ensure all tests pass
   - Follow existing code style
   - Add appropriate comments

3. **Documentation**
   - Update README.md if needed
   - Add/update XML documentation
   - Update configuration examples

### Review Process

1. **Automated Checks**
   - Build must pass
   - Tests must pass
   - Code style checks

2. **Manual Review**
   - Code quality and style
   - Security considerations
   - Performance implications
   - Documentation completeness

## 🔒 Security

### Reporting Security Issues

**Do not** open public issues for security vulnerabilities. Instead:

1. Email security issues to: security@yourdomain.com
2. Include detailed information about the vulnerability
3. Allow reasonable time for response before public disclosure

### Security Guidelines

- Never commit sensitive data (API keys, passwords, etc.)
- Use environment variables for configuration
- Validate all user inputs
- Follow OWASP guidelines for web security

## 📚 Documentation

### Code Documentation

- Use XML documentation for public APIs
- Include parameter descriptions
- Provide usage examples where helpful

```csharp
/// <summary>
/// Authenticates a user with the provided credentials.
/// </summary>
/// <param name="username">The username to authenticate</param>
/// <param name="password">The password for the user</param>
/// <returns>Authentication result with tokens if successful</returns>
public async Task<AuthenticationResult> AuthenticateAsync(string username, string password)
```

### README Updates

When adding features or changing behavior:
- Update the README.md
- Add configuration examples
- Update API documentation
- Include any new dependencies

## 🏷️ Release Process

### Version Numbering

We use [Semantic Versioning](https://semver.org/):
- **MAJOR**: Breaking changes
- **MINOR**: New features (backward compatible)
- **PATCH**: Bug fixes (backward compatible)

### Release Checklist

- [ ] All tests pass
- [ ] Documentation updated
- [ ] Version numbers updated
- [ ] Changelog updated
- [ ] Docker image built and tested
- [ ] Release notes prepared

## 💬 Communication

### Getting Help

- **Issues**: Use GitHub Issues for bugs and feature requests
- **Discussions**: Use GitHub Discussions for questions and ideas
- **Code Review**: Use PR comments for specific code feedback

### Community Guidelines

- Be respectful and inclusive
- Provide constructive feedback
- Help others learn and grow
- Follow the [Code of Conduct](CODE_OF_CONDUCT.md)

## 📄 License

By contributing to StashPlayaVR API, you agree that your contributions will be licensed under the MIT License.

## 🙏 Recognition

Contributors will be recognized in:
- CONTRIBUTORS.md file
- Release notes
- Project documentation

Thank you for contributing to StashPlayaVR API! 🎉
