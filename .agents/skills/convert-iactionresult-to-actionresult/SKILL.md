---
name: convert-iactionresult-to-actionresult
description: >
  Refactors ASP.NET Core controller action methods from returning generic `IActionResult` or `Task<IActionResult>` 
  to strongly-typed `ActionResult<T>` or `Task<ActionResult<T>>`. Updates method signatures, implicit 200 OK returns, 
  and Swagger/OpenAPI annotations.
---

# Skill: Convert IActionResult to ActionResult<T>

## Objective
Refactor ASP.NET Core API controller methods returning `IActionResult` or `Task<IActionResult>` to strongly-typed `ActionResult<T>` or `Task<ActionResult<T>>`. This improves compile-time type safety, enables direct return of payloads for successful responses, and optimizes Swagger/OpenAPI documentation generation.

---

## Refactoring Rules & Execution Protocol

### 1. Identify Target Payload Type (`T`)
* Inspect the successful return path of the controller action (e.g., `return Ok(myObject);` or `return Ok(new MyDto());`).
* Determine the exact type `T` inside `Ok(...)`.
* Change the return signature:
  * `Task<IActionResult>` → `Task<ActionResult<T>>`
  * `IActionResult` → `ActionResult<T>`

### 2. Simplify 200 OK Success Returns
* Locate where `Ok(payload)` is called for successful executions.
* Replace `return Ok(payload);` with `return payload;`.
* **Reasoning**: ASP.NET Core implicitly converts an instance of `T` to `ActionResult<T>` with an HTTP 200 OK status code.

### 3. Handle Non-200 Status Codes & Error Paths
* Keep standard framework helpers for non-200 responses unchanged (e.g., `return BadRequest(error);`, `return NotFound();`, `return Unauthorized();`).
* **Reasoning**: `ActionResult<T>` defines implicit conversion operators for `ActionResult` types, allowing error responses to pass seamlessly without manual casting.

### 4. Clean Up Swagger Attributes (`[ProducesResponseType]`)
* **Remove** `[ProducesResponseType(typeof(T), StatusCodes.Status200OK)]` or `[ProducesResponseType(200)]` above the target method.
  * *Why*: `ActionResult<T>` automatically tells OpenAPI generators (Swashbuckle/NSwag) the type of the 200 OK response payload.
* **Retain or Add** `[ProducesResponseType]` attributes for any non-200 HTTP status codes explicitly returned in the body (e.g., 400 Bad Request, 404 Not Found, 500 Internal Server Error).

---

## Input / Output Examples

### Example 1: Standard Success with Error Handling

#### Input Code (Before)
```
[HttpGet("{id}")]
[ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> GetProduct([FromRoute] string id)
{
    var product = await _productService.GetByIdAsync(id);
    if (product == null)
    {
        return NotFound();
    }
    return Ok(product);
}
```

#### Output Code (After)
```
[HttpGet("{id}")]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<ActionResult<ProductDto>> GetProduct([FromRoute] string id)
{
    var product = await _productService.GetByIdAsync(id);
    if (product == null)
    {
        return NotFound();
    }
    return product;
}
```

### Example 2: Wrapper Class Return Type (ApiActionResult<T>)

#### Input Code (Before)
```
[HttpPost("crawl/{stockId}")]
[ProducesResponseType(typeof(ApiActionResult<DividendCrawlResult>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ApiActionResult<DividendCrawlResult>), StatusCodes.Status400BadRequest)]
public async Task<IActionResult> RequestCrawl([FromRoute] string stockId)
{
    if (string.IsNullOrWhiteSpace(stockId))
    {
        return BadRequest(new ApiActionResult<DividendCrawlResult> { IsSuccess = false, Message = "Invalid ID" });
    }

    var result = await _service.CrawlAsync(stockId);
    return Ok(new ApiActionResult<DividendCrawlResult> { Payload = result, IsSuccess = true });
}
```

#### Output Code (After)
```
[HttpPost("crawl/{stockId}")]
[ProducesResponseType(typeof(ApiActionResult<DividendCrawlResult>), StatusCodes.Status400BadRequest)]
public async Task<ActionResult<ApiActionResult<DividendCrawlResult>>> RequestCrawl([FromRoute] string stockId)
{
    if (string.IsNullOrWhiteSpace(stockId))
    {
        return BadRequest(new ApiActionResult<DividendCrawlResult> { IsSuccess = false, Message = "Invalid ID" });
    }

    var result = await _service.CrawlAsync(stockId);
    return new ApiActionResult<DividendCrawlResult> { Payload = result, IsSuccess = true };
}
```

---

## Validation Checklist

Before outputting the converted code, verify:

1. Does the return type in the method signature strictly match the object type returned on the success execution path?
2. Were all `return Ok(data);` statements converted to direct `return data;` statements?
3. Were all 200 OK `[ProducesResponseType]` attributes removed?
4. Are non-200 HTTP response codes (400, 404, etc.) still using appropriate helper methods (`BadRequest()`, `NotFound()`) and documented with `[ProducesResponseType]`?
