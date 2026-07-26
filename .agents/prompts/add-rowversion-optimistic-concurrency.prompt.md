---
description: "Add EF Core RowVersion/optimistic concurrency to a database entity and wire it into the matching PUT flow."
agent: "agent"
argument-hint: "Entity name or table name, for example StockPortfolio"
---

Apply the RowVersion pattern to one database entity at a time in this repository.

Context:
- Entity classes live under api/Database.
- EF Core entity configurations live under api/Database/EntityConfigurations.
- PUT DTOs and controllers are under api/Controllers.

When an entity name or table name is provided:
1. Find the entity class in api/Database.
2. Add `public uint Version { get; set; }` to the entity class.
3. Find the matching configuration in api/Database/EntityConfigurations and add `builder.Property(e => e.Version).IsRowVersion();`.
4. Find the related DTO (usually `{EntityName}PutDto` or `{TableName}PutDto`) and the controller that handles the corresponding `HttpPut` and `HttpGet` action.
5. Add a `version` field to the DTO if it does not already exist.
6. In the PUT action, set the concurrency token before saving changes:
   `context.Entry(entity).Property(e => e.Version).OriginalValue = dto.version;`
7. Keep the change scoped to one table/entity at a time.
8. Verify the result by building the API project with `dotnet build api/StockHub.csproj`.

Prefer minimal, consistent edits that match the existing pattern already used for the stock entity and its controller.

If no PUT flow exists for the entity, still add the RowVersion property and configuration, then report that the concurrency wiring was not applicable for that entity.
