# Library Management System

Public library management system built with .NET 8 Web API and PostgreSQL.

## Features

- **Books**: Add and search books by category and availability
- **Members**: Register library members
- **Loans**: Borrow and return books with fine calculation
- **Business Rules**: Max 5 active loans, 14-day loan period, $0.50/day fine

## Tech Stack

- **.NET 8** Web API
- **PostgreSQL** with Entity Framework Core
- **Swagger** UI for API documentation
- **xUnit** + AutoFixture + FluentAssertions for testing
- **Testcontainers** for integration and database tests
- **k6** for performance testing
- **GitHub Actions** CI/CD

## API Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/books` | Get all books (filter: category, available) |
| POST | `/api/books` | Add a book |
| GET | `/api/members` | Get all members |
| POST | `/api/members` | Register a member |
| POST | `/api/loans` | Borrow a book |
| POST | `/api/loans/{id}/return` | Return a book |
| GET | `/api/members/{id}/loans` | Get member's loan history |
| GET | `/api/loans/overdue` | Get all overdue loans |

## Running Locally

```bash
# Restore and build
dotnet restore
dotnet build

# Run API (requires PostgreSQL)
dotnet run --project src/LibraryManagement.Api

# Swagger UI available at http://localhost:5126
```

## Running Tests

```bash
# Unit tests
dotnet test tests/LibraryManagement.UnitTests

# Integration tests (requires Docker)
dotnet test tests/LibraryManagement.IntegrationTests

# Database tests (requires Docker)
dotnet test tests/LibraryManagement.DatabaseTests
```

## Performance Tests (k6)

```bash
# Book search load test
k6 run tests/LibraryManagement.PerformanceTests/book-search-load.js

# Concurrent borrow stress test
k6 run tests/LibraryManagement.PerformanceTests/concurrent-borrow-stress.js
```
