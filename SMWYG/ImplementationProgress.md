# Development Progress Log

## Environment Snapshot
- Target framework: .NET 10 (WPF client + ASP.NET Core backend when introduced)
- Database: PostgreSQL (Docker) via connection string `Host=localhost;Port=5433;Database=smwyg;Username=admin;Password=17071998`
- ORM: Entity Framework Core with `AppDbContext`

## Phase Timeline
The project follows the phased plan defined in `Project-guide.md`. Each phase summary below captures completed work, open tasks, and notes for future contributors.

### Phase 1 – Server Creation & Customization (current focus)
- [x] Users can create servers (auto-joins owner, seeded text + voice channels).
- [x] Users can send plain text messages inside text channels.
- [x] Allow updating server name from the client UI (persisted via API/DbContext).
- [x] Allow selecting/uploading a server icon and storing the icon path/URL (icons copied into `ServerIcons/`).
- [x] Provide a consolidated server settings drawer (invite code preview, rename/icon management, delete).
- [x] Allow creating additional channels (text or voice) per server from the client UI.
- [x] Channel creation now uses a dialog with radio buttons for text vs. voice; remove channel is a single-click action.
- [x] Allow renaming existing channels (respecting uniqueness per server).
- [x] Allow deleting servers (with confirmation + cascading cleanup via EF Core).
- [x] Allow deleting channels per server (with confirmation messages).
- [x] Add light validation/error notifications around channel/server operations.
- [x] Ensure server/user icons render when paths are set (new `RelativePathToImageConverter`).
- [x] Channel list shows dedicated text/voice icons plus additional spacing for readability.
- [x] Persist channel `position` ordering when users reorder channels (optional stretch).
- [x] Replaced the remaining `Interaction.InputBox` prompts with themed dialogs for server creation and channel renaming.

> Latest update: Added a compact server settings drawer (invite code + rename/icon/delete), simplified the channel actions bar (dialog-driven add + quick remove), introduced a user settings panel for updating username/profile picture with a placeholder logout flow, refreshed the channel list visuals (spacing + icons), stood up the Super Admin invite-token manager overlay, finished channel reordering with persisted positions plus invite-token-backed registration and admin delete/deactivate flows, and aligned server/channel prompts with the new dialog style.

### Phase 2 – User Management Panel (next)
- [x] Build Super Admin panel for invite token lifecycle (generate, view, revoke).
- [x] Hook up invite-token-based registration/login flows.
- [x] Foundation: client-side user settings flyout lets the signed-in user rename themselves and update avatars (persists to `users`).
- [x] Allow super admins to delete/deactivate users and cascade membership cleanup.

## Next Actions
1. Confirm how/where server icons should be stored in production (local folder vs. CDN/blob) and adjust the current `ServerIcons/`/`ProfilePictures/` copy logic accordingly.
2. Implement the documented invite-token redemption/deactivation endpoints on the ASP.NET Core backend and surface their responses to the WPF client.
3. Expand auth planning beyond invite tokens (password reset, enforced password change on first login, audit retention policy).
4. Add automated tests around channel/server mutations once the backend API surface replaces the current direct `AppDbContext` usage.

## Backend/API Workflow – Invite Tokens & Deactivation Auditing

### Invite Token Redemption
1. Endpoint: `POST /api/invite-tokens/redeem` accepts `{ token, username, password }`.
2. Validate token from `invite_tokens` (exists, not expired, `max_uses` > `uses`, `is_used = false`).
3. Wrap in transaction: create user (hash password, default profile picture), attach to default server(s) if the token scopes one, increment `uses` and set `is_used` when `uses == max_uses`.
4. Persist an audit row in `invite_token_audits` `{ id, token_id, action = "redeemed", actor = user_id, ip, user_agent, occurred_at }`.
5. Return JWT/session plus lightweight profile payload so the WPF client can hydrate immediately.

### Invite Token Deactivation & Auditing
1. Endpoint: `POST /api/invite-tokens/{id}/deactivate` requires Super Admin JWT.
2. Server sets `is_used = true`, `max_uses = 0`, `expires_at = now`, and records `deactivated_by` (admin id) + optional `reason`.
3. Append audit row `{ action = "deactivated", actor = admin_id, reason }` for traceability.
4. Surface audit history through `GET /api/invite-tokens/{id}/audit` so the client overlay can show who revoked a code and why.
5. Extend user deactivation endpoint to log `{ action = "user_deactivated", target_user, actor, reason }` so future compliance reviews can pair invite usage with account lifecycle changes.
