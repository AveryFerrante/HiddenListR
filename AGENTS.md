# HiddenListR - Agent Guide

## Project Overview
HiddenListR is a web application for small groups (initially family use) to manage Christmas gift lists while preserving surprise.

## Core Goals
- Let users maintain personal wish lists.
- Let users view other people's wish lists.
- Let users mark items on other people's lists as purchased.
- Ensure list owners cannot see purchase status on their own items.

## Planned MVP Features
- **Accounts (v1, simple):** Users identify with a phone number (no master password). Simple/trusted-family, not robust production auth.
- **Wish list management:** Users can create/edit/remove items on their own list. (Create/edit implemented on event details; remove not yet.)
- **Wish list item fields:** At minimum title, description, and product link/URL; allow room for additional basic metadata.
- **List browsing:** Users can view other users' lists within events they belong to.
- **Purchase tracking:** Users can claim items on others' lists. Claim state must be hidden from the list owner in all UI/API responses.

## Key Product Constraint
**Surprise integrity is critical:** never expose purchased/claimed status of an item to that item's owner. Treat this as a hard requirement in data modeling, queries, and API design.

## Current Repository State
- Git repo initialized on `main` (no commit, files currently untracked).
- .NET 10 Razor Pages app in `HiddenListR/` with `HiddenListR.Domain/` class library for EF Core/SQLite.
- Solution: `HiddenListR.slnx` includes web + domain projects.
- `HiddenListR/Program.cs` wires domain via `AddDomain(connectionString)`; default SQLite connection in `appsettings.json`; Razor Pages maps `/` to `/Events/Index` (no standalone home page).
- `docker-compose.yml`: `db-init` runs EF migrations + `HiddenListR.Domain/Scripts/*.sql` seed scripts, then `hiddenlistr` starts. Persisted SQLite volume `hiddenlistr-data` at `/app/data`.
- Build: `dotnet` not in WSL PATH; use `powershell.exe -NoProfile -Command "cd 'C:\Users\Avery\source\repos\HiddenListR'; dotnet build HiddenListR.slnx ..."`. EF migrations exist in `HiddenListR.Domain/Migrations`.

## Domain Model (current)
- **User**: `Id` (int, PK), `FirstName`, `LastName`, `PhoneNumber` (unique), `Role` (`UserRole` enum, stored as int; `User`/`Admin`), navs `EventMembers`, `WishLists`, `Purchases`.
- **Event**: `Id` (int, PK), `Title`, `CreatedDateUtc`, `ExpireDateUtc?`, navs `EventMembers`, `WishLists`.
- **EventMember**: join `UserId`+`EventId` (composite PK), navs `User`, `Event`. Represents event membership.
- **WishList**: `Id` (int, PK), `EventId`, `OwnerUserId`, `CreatedDateUtc`, `UpdatedDateUtc?`; unique `(EventId, OwnerUserId)`; navs `Event`, `OwnerUser`, `Items`.
- **WishListItem**: `Id` (int, PK), `WishListId`, `Title`, `Description?`, `ProductUrl?`, `CreatedDateUtc`, `UpdatedDateUtc?`; nav `WishList`, optional `Purchase`.
- **WishListItemPurchase**: `Id` (int, PK), `WishListItemId` (unique), `PurchasedByUserId`, `PurchasedDateUtc`; navs `WishListItem`, `PurchasedByUser`. **Never expose to list owner.**

## Auth & Authorization
- Cookie auth (`hlr.auth`, 24h, persistent, sliding, HttpOnly, SameSite Lax, `CookieSecurePolicy.SameAsRequest`). Claims: `NameIdentifier` (int user id), `Name`, `Role`.
- Login: phone-only lookup via `/api/auth/login` (`[IgnoreAntiforgeryToken]`), no master password/localStorage auth state. Logout: `/api/auth/logout` signs out and redirects `/Login`.
- Fallback authorization requires authenticated user. Anonymous: `/Login/Index`, `/Api/Auth`, `/Error`. API unauthenticated => 401; pages redirect `/Login`.
- Middleware order: `UseAuthentication -> UseMiddleware<UserContextMiddleware> -> UseAuthorization`. `RequestUserContext` (scoped) holds current `User`; `UserContextMiddleware` resolves by int claim.
- Admin-only pages use `[AdminOnly]` attribute (`HiddenListR/Auth/AdminOnlyAttribute.cs`) with `AdminOnly` policy registered in `Program.cs`.
- Shared-page role checks use `User.IsAdmin()` extension (`HiddenListR.Domain/Entities/UserExtensions.cs`); `Pages/_ViewImports.cshtml` imports `HiddenListR.Domain.Entities`.

## Implemented Features
- **Events page (`/Events`)**: admin sees all events + create form and member management; normal users see only member events. Events render as Oat cards with title, member list, Active/Expired badge (no raw dates), and a lower-right "View Event Info" link. If a normal user belongs to exactly one event, GET `/Events` redirects to that details page and the details page hides "Back to Events".
- **Admin event member management**: on Events page, admin-only modal (Oat/native `<dialog>`) to add/remove members. Remove warns when users have wish list items for the event; confirm cascades delete of that user's `WishList` for the event (and items/purchases via DB cascade/model behavior as configured). Server-side admin checks on all handlers.
- **Users page (`/Users`)**: `[AdminOnly]`; list users with events they belong to; create user form (First/Last/Phone/Role, default `User`), duplicate phone blocked.
- **Event details (`/Events/Details/{id}`)**: requires authentication; normal users must be event members, admins may view any event. An Oat `ot-dropdown` labeled "Viewing {CurrentViewLabel}" chooses whose list to see via `?active_view={userId}` or `my-claims` (defaults to the signed-in user; invalid ids fall back to own list); an `<hr>` separates "My Claimed Items" from other members. "My List" never loads/shows claim state (only the total-items badge); another member's option (`{FirstName} {LastInitial}'s List - X items`) shows claimed/not-claimed/total Oat badges and shared item cards. Claim/unclaim controls appear only for event members; they always act as the signed-in user. "My Claimed Items" lists only the viewer's claims across the event, shows the owner as small parenthetical text beside the card title, and has no claim-status badges. Cards claimed by the viewer have an "Unclaim" POST button (available in both other-member and my-claims views) that removes only that viewer's purchase. POST redirects preserve `active_view` so the same list stays selected. Own list has the "Add New Item" dialog; admins viewing another member's list also get an "Add Item for {name}" dialog (Title/Description/ProductUrl) with a hidden `ownerUserId`. Normal users can only add to their own list; admins may add items for any event member but cannot claim/unclaim for anyone else. Each card has an "Edit" button opening a native `<dialog>` with title/description/product URL; owners can edit their own items and admins can edit on behalf of any event member, with no delete option. Card descriptions render under an underlined "More Info:" label and are omitted entirely when empty; the product link is a regular anchor with `wwwroot/assets/open_link.png` appended as a small icon. Add/Edit form inputs are bound per-handler via `[FromForm(Name = "Input")]` / `[FromForm(Name = "EditInput")]` rather than global `[BindProperty]`, so unrelated POSTs don't trigger each other's validation.
- **Claim tracking**: `WishListRepository.GetItemsForOtherUsersAsync` returns viewer-relative claim state (`ItemClaimState`); owner-list queries never load it. `ClaimItemAsync` blocks self-claims, duplicates, and non-event items; `UnclaimItemAsync` removes only the signed-in user's purchase on an event item owned by someone else. Claimed items show an Oat badge ("already claimed" or green "I've claimed this"); the button is hidden once claimed.
- **UI**: Oat UI included in shared layout and login page (`@@knadh/oat` CSS/JS CDN), `<html data-theme="dark">`. Header shows "My Events" for normal users / "Events" for admins, the signed-in user's name left of Logout, no Home link, and an About link; footer reads "- About". About page replaces the old Privacy page. Page-specific JS loaded via Razor sections (`wwwroot/js/events.js`); dialogs use Oat's native `<dialog>` structure with no custom CSS.

## Key Files
- `HiddenListR/Program.cs` - auth, authorization policies, middleware, Razor Pages conventions.
- `HiddenListR/Auth/AdminOnlyAttribute.cs` - admin-only page attribute/policy hook.
- `HiddenListR/Middleware/UserContextMiddleware.cs`, `HiddenListR/Services/RequestUserContext.cs` - current user resolution.
- `HiddenListR.Domain/Entities/*` - EF entities; `UserExtensions.IsAdmin()`.
- `HiddenListR.Domain/Data/AppDbContext.cs` - EF model/config; no Guid converters (int PKs).
- `HiddenListR.Domain/Repositories/*` - `UserRepository`, `EventRepository`, `WishListRepository`; `WishListRepository` has owner-scoped `GetItems*`, `AddItemAsync`, `UpdateItemAsync`, `ClaimItemAsync`, `UnclaimItemAsync`; registered in `DependencyInjection.cs`.
- `HiddenListR/Pages/Events/Index*` - events list, admin create/manage members.
- `HiddenListR/Pages/Events/Details*` - member event details, my wish list add/view, other wish lists.
- `HiddenListR/Pages/Users/Index*` - admin user management.
- `HiddenListR/Pages/About*` - general site description; replaces old Privacy page.
- `HiddenListR/wwwroot/assets/open_link.png` - small icon appended to product links.
- `HiddenListR/Pages/Login/Index*`, `HiddenListR/Pages/Api/Auth/*` - auth.
- `HiddenListR.Domain/Scripts/run-db-init.sh`, `users_seed.sql` - Docker DB init/seeds (idempotent seeds).

## Docker/DB Notes
- `db-init` runs as root; script chmods `/app/data` and sqlite files after init so app user can write. Existing volumes created before this fix may need: `docker compose exec -u root hiddenlistr sh -lc "chmod 666 /app/data/hiddenlistr.db /app/data/hiddenlistr.db-wal /app/data/hiddenlistr.db-shm"`.
- EF migration `20261122044841_InitialCreate` is int-PK based. Recreating DB: `docker compose down -v && docker compose up --build`.

## Known Gaps / Next Steps
- **Wish list item delete**: no delete UI/handler yet; consider constraints once purchases exist.
- **Validation/UX polish**: client/server validation display, better empty states/styling with Oat, error handling.
- **Security hardening**: CSRF is used on normal forms; JSON handlers use antiforgery token header - verify coverage; consider phone normalization/constraints.
- **Tests/lint**: no project lint/typecheck/test commands established in repo; ad-hoc Docker smoke checks were used during development but are not committed. Add when introducing tests.
