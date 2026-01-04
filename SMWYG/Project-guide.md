This is a discord-clone application that is meant for streaming.
It uses a postgresSQL DB to store user data and streams.

Things this App should do:

  * Allow super admins to generate invite tokens that users can use to register/login. No Authentication system beyond this.
  * Allow users to create and join "servers" (like Discord servers).
  * Allow users to create text channels within servers for chat.
  * Allow users to send and receive messages in text channels.
  * Allow users to send gifs and images in text channels.
  * Allow users to create voice channels within servers for streaming calls.
  * Allow users to start and stop streaming in voice channels.
  * Allow users to stream in a variety of quality settings (e.g., 720p, 1080p).
  * Allow Users to have profile pictures and display names.
  * Allow users to to customize their server with icons and change the Server Names.
  * Allow server owners to manage members (e.g., promote to admin, kick).
  
Implement the database schema to support these features.
Implement the above requirements in phases, start with a Admin panel to generate invite tokens, then user registration/login, then server and channel creation, and so on.

Here is the PostgresSQL schema for the application:
```sql
-- Enable UUID extension (for generating unique IDs)
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- ===================================
-- 1. Users Table
-- ===================================
CREATE TABLE users (
    id              UUID        PRIMARY KEY DEFAULT uuid_generate_v4(),
    username        VARCHAR(50) UNIQUE NOT NULL,
    display_name    VARCHAR(100),
    password_hash   VARCHAR(255) NOT NULL,  -- Store BCrypt/Argon2 hash, never plain text
    profile_picture VARCHAR(255),           -- URL or file path to avatar
    created_at      TIMESTAMPTZ DEFAULT NOW(),
    is_admin        BOOLEAN     DEFAULT FALSE   -- Only super admin(s) have this true
);

-- ===================================
-- 2. Invite Tokens Table
-- Simple token auth: Super admin generates a token, user uses it once to register/login
-- ===================================
CREATE TABLE invite_tokens (
    id              UUID        PRIMARY KEY DEFAULT uuid_generate_v4(),
    token           VARCHAR(64) UNIQUE NOT NULL,  -- Random secure string (e.g., 32-64 chars)
    created_by      UUID        NOT NULL REFERENCES users(id),
    used_by         UUID        REFERENCES users(id),     -- NULL if not used yet
    is_used         BOOLEAN     DEFAULT FALSE,
    created_at      TIMESTAMPTZ DEFAULT NOW(),
    expires_at      TIMESTAMPTZ,                      -- Optional expiration
    max_uses        INTEGER     DEFAULT 1             -- Usually 1 for single-use
);

-- ===================================
-- 3. Servers Table
-- ===================================
CREATE TABLE servers (
    id              UUID        PRIMARY KEY DEFAULT uuid_generate_v4(),
    name            VARCHAR(100) NOT NULL,
    icon            VARCHAR(255),                     -- Optional server icon URL
    owner_id        UUID        NOT NULL REFERENCES users(id),
    created_at      TIMESTAMPTZ DEFAULT NOW()
);

-- ===================================
-- 4. Server Members (Many-to-Many: Users ↔ Servers)
-- ===================================
CREATE TABLE server_members (
    server_id       UUID NOT NULL REFERENCES servers(id) ON DELETE CASCADE,
    user_id         UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    joined_at       TIMESTAMPTZ DEFAULT NOW(),
    role            VARCHAR(20) DEFAULT 'member',  -- e.g., 'admin', 'moderator', 'member'
    PRIMARY KEY (server_id, user_id)
);

-- ===================================
-- 5. Channels Table
-- ===================================
CREATE TABLE channels (
    id              UUID        PRIMARY KEY DEFAULT uuid_generate_v4(),
    server_id       UUID        NOT NULL REFERENCES servers(id) ON DELETE CASCADE,
    name            VARCHAR(100) NOT NULL,
    type            VARCHAR(20) NOT NULL CHECK (type IN ('text', 'voice')),  -- 'voice' for streaming calls
    category        VARCHAR(100),                     -- Optional grouping (e.g., "General", "Gaming")
    position        INTEGER     DEFAULT 0,            -- For ordering channels
    created_at      TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(server_id, name)  -- Prevent duplicate channel names per server
);

-- ===================================
-- 6. Messages Table (Text channels only)
-- ===================================
CREATE TABLE messages (
    id              UUID        PRIMARY KEY DEFAULT uuid_generate_v4(),
    channel_id      UUID        NOT NULL REFERENCES channels(id) ON DELETE CASCADE,
    author_id       UUID        NOT NULL REFERENCES users(id),
    content         TEXT        NOT NULL,
    sent_at         TIMESTAMPTZ DEFAULT NOW(),
    edited_at       TIMESTAMPTZ,
    deleted_at      TIMESTAMPTZ  -- Soft delete
);

-- Optional: Add indexes for performance
CREATE INDEX idx_messages_channel_sent ON messages(channel_id, sent_at DESC);
CREATE INDEX idx_server_members_user ON server_members(user_id);
CREATE INDEX idx_channels_server ON channels(server_id);

-- ===================================
-- Optional: Active Screen Share / Voice Sessions (Bonus)
-- If you want to track who is currently streaming in a voice channel
-- ===================================
CREATE TABLE active_streams (
    id              UUID        PRIMARY KEY DEFAULT uuid_generate_v4(),
    channel_id      UUID        NOT NULL REFERENCES channels(id) ON DELETE CASCADE,
    streamer_id     UUID        NOT NULL REFERENCES users(id),
    started_at      TIMESTAMPTZ DEFAULT NOW(),
    ended_at        TIMESTAMPTZ,
    UNIQUE(channel_id)  -- Only one active streamer per voice channel at a time
);
````