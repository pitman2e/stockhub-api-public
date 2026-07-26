---
name: validation-boundary-review
description: Review codebase architecture for clean validation boundary separation across Controllers, FluentValidation, Services, and Repositories. Use when reviewing code, auditing API logic, or checking proper placement of validation rules.
disable-model-invocation: true
---

# Validation Boundary Review

Review backend application code to ensure validation rules and responsibilities are cleanly separated according to clean architecture principles across FluentValidation, Services, and Repositories.

## Validation Architecture Boundaries

### 1. Controllers & FluentValidation (Input Edge / Presentation Layer)
- **Scope**: Format & Constraint Validation, Cross-Property Structural Rules.
- **Responsibilities**:
  - String length bounds, numeric precision, non-empty, regex formats.
  - Cross-property dependent validations (e.g., conditional requirement checks).
  - Pure request payload integrity without database side-effects.
- **Rules & Best Practices**:
  - Keep FluentValidation classes **stateless and DB-free**.
  - Do NOT inject `DbContext` or query repositories inside FluentValidation rules.
  - Return HTTP 400 Bad Request with standardized error payloads when FluentValidation fails.

### 2. Services (Application / Domain Layer)
- **Scope**: Domain Rules, Business Logic, Referential Integrity, State Transitions.
- **Responsibilities**:
  - **Referential Integrity**: Verifying foreign keys, entity existence (e.g., currency exists in DB, stock exists).
  - **Business Rules**: Domain invariants and business constraints (e.g., virtual portfolio rules, parent-child relationships).
  - **State Transitions**: Validating workflow state changes before applying updates.
- **Rules & Best Practices**:
  - Throw domain-specific exceptions when business checks fail.
  - Do NOT leak API or UI response formatting logic into services.

### 3. Repositories (Persistence Layer)
- **Scope**: Data Access Mechanics, State Persistence & Concurrency Tracking.
- **Responsibilities**:
  - Database queries, adding/updating/deleting entities (`DbContext.SaveChangesAsync()`).
  - **State & Concurrency**: Enforcing optimistic concurrency mechanics (e.g., EF Core `RowVersion` / `Version` property tracking).
- **Rules & Best Practices**:
  - Do NOT place business rule validations inside repository methods.
  - Keep repository methods focused purely on data access operations.

## Checklist for Code Review

- [ ] Are FluentValidation rules completely free of DB access or injected `DbContext`?
- [ ] Is input format/constraint validation handled before reaching business services?
- [ ] Are foreign key checks and entity existence checks placed in the Service layer?
- [ ] Are business domain logic and entity invariants enforced in Services rather than Repositories or Controllers?
- [ ] Is optimistic concurrency control (e.g., version tracking) handled properly during persistence operations?

## Review Workflow

1. **Scan Controllers & Validators**: Check if FluentValidation is used strictly for format, types, and cross-field structural checks. Identify any DB calls that need to be refactored into Services.
2. **Scan Services**: Check if domain rules, referential integrity checks, and business logic are properly placed in the application/domain layer and throw domain exceptions.
3. **Scan Repositories**: Verify repositories are lean data access components enforcing persistence and concurrency mechanics without carrying business rules.
4. **Provide Output**: Summarize compliance, highlight any misplaced validation rules, and offer concrete code refactoring suggestions.
