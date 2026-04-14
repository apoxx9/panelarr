# Contributing to Panelarr

Thanks for your interest in contributing! Here's how you can help.

## Reporting Bugs

Open a [GitHub Issue](https://github.com/apoxx9/panelarr/issues) with:
- Steps to reproduce
- Expected vs actual behavior
- Panelarr version and platform (Docker, OS, etc.)
- Relevant log output (Settings > General > Log Level: Debug)

## Feature Requests

Open a [GitHub Issue](https://github.com/apoxx9/panelarr/issues) with a description of the feature and why it would be useful.

## Development

### Prerequisites

- .NET 10.0 SDK
- Node.js 20+
- Yarn

### Building

```bash
# Backend
cd src
dotnet build Panelarr.sln

# Frontend
cd frontend
yarn install
yarn build

# Run
dotnet run --project NzbDrone.Console --framework net10.0
```

### Running Tests

```bash
# Backend tests
cd src
dotnet test Panelarr.sln

# Frontend lint
cd frontend
yarn lint
```

### Pull Requests

1. Fork the repo and create a branch from `main`
2. Make your changes
3. Ensure tests pass and the build is clean
4. Open a pull request with a clear description of the change

## License

By contributing, you agree that your contributions will be licensed under the [GNU GPL v3](LICENSE.md).
