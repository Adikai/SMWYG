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
- [ ] Persist channel `position` ordering when users reorder channels (optional stretch).

> Latest update: Added a compact server settings drawer (invite code + rename/icon/delete), simplified the channel actions bar (dialog-driven add + quick remove), and introduced a user settings panel for updating username/profile picture with a placeholder logout flow.

### Phase 2 – User Management Panel (next)
- [ ] Build Super Admin panel for invite token lifecycle (generate, view, revoke).
- [ ] Hook up invite-token-based registration/login flows.
- [x] Foundation: client-side user settings flyout lets the signed-in user rename themselves and update avatars (persists to `users`).
- [ ] Allow super admins to delete/deactivate users and cascade membership cleanup.

## Next Actions
1. Design and implement channel drag/drop (or ordering controls) that persist `position` back to the database.
2. Replace the remaining InputBox prompts (server creation, channel rename) with richer dialogs consistent with the new panels.
3. Start outlining the Super Admin invite-token workflow (Phase 2) so the backend API surface can be defined early.
4. Confirm how/where server icons should be stored in production (local folder vs. CDN/blob) and adjust the current `ServerIcons/`/`ProfilePictures/` copy logic accordingly.

Document progress updates in this file after each meaningful change so future contributors and LLMs can pick up where we left off.
