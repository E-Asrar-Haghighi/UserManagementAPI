# TechHive User Management API

A robust, enterprise-grade ASP.NET Core Web API built with a focus on thread-safety, scalability, and security.

## 🚀 The Development Journey

This project evolved through three distinct phases of engineering:

### 1. Foundation (Phase 1)
- **Objective**: Establish core CRUD functionality.
- **Action**: Scaffolded Minimal API endpoints for creating, retrieving, updating, and deleting users.
- **Storage**: Initialized with a basic in-memory list.

### 2. Optimization & Robustness (Phase 2)
- **Thread Safety**: Migrated the data store to `ConcurrentDictionary` to prevent race conditions during concurrent requests.
- **Data Transfer Objects (DTOs)**: Decoupled the internal `User` model from the public API using DTOs, improving security and contract stability.
- **Scalability**: Implemented pagination on the `GET /users` endpoint to handle large datasets efficiently.
- **Centralized Validation**: Unified validation logic using `DataAnnotations` to ensure data integrity across all endpoints.

### 3. Enterprise Infrastructure (Phase 3)
- **Standardized Error Handling**: Built a global exception handler that catches unhandled errors and returns a consistent, user-friendly JSON format: `{ "error": "Internal server error." }`.
- **JWT Security**: Protected all sensitive endpoints using token-based authentication. Users must log in via `/login` to receive a secure Bearer token.
- **Audit Logging**: Engineered a custom middleware that tracks every request and response, logging methods, paths, and status codes for corporate compliance.
- **Pipeline Strategy**: Optimized the middleware order (Error Handler -> Auth -> Logger) to ensure the API is safe, secure, and audited in the correct sequence.

## 🛠️ Procedures Taken

- **Incremental Refactoring**: Each phase built upon the previous one without breaking existing logic (verified by regression testing).
- **Security-First Design**: Ensured internal IDs and data structures are never exposed directly.
- **Middleware Orchestration**: Layered the request pipeline to provide a "fail-fast" security gate followed by detailed auditing.

## ✅ How We Validated Everything

We utilized a "three-pillar" validation strategy to ensure production readiness:

1. **The Automated Test Suite (`test_api.py`)**:
   - A comprehensive Python-based test runner that simulates the entire lifecycle.
   - **Tests**: Unauthorized access blocking, JWT login flow, paginated data retrieval, and full CRUD operations.
   - **Verification**: Asserted that response codes (200, 201, 204, 400, 401, 404, 500) matched the expected business logic.

2. **Controlled Exception Testing**:
   - Specifically created a debug endpoint to throw unhandled exceptions.
   - **Goal**: Verify that the API never crashes and always returns the standardized "Internal server error" JSON.

3. **Manual Audit Console Check**:
   - Monitored the server console during active testing.
   - **Goal**: Confirm that the logging middleware accurately recorded every interaction in real-time.

---
**TechHive Solutions - User Management API Project**
