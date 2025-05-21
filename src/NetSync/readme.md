# NetSync workings
NetSync is a library that provides a way to synchronize data between different devices or systems. It is designed to be lightweight and easy to use, making it suitable for a wide range of applications.

The main focus of NetSync is providing a way to distribute data over a local network, rather than using a third-party system over the internet. E.g.:  Distributed Cache and Document Stores.

## Main components
- Discovery
  - Promotes the discovery of devices on the network using udp and tcp.
  - Uses a simple protocol to announce the presence of devices and to discover other devices on the network.
- IMessaging (Messaging)
  - Implements the messaging protocol for sending and receiving messages between devices.
- SyncData
  - Provides a way to synchronize data between devices.
  - Uses a simple protocol to send and receive data between devices.
- NetworkService
  - binds the Discovery to the IMessaging components for network communcation.

## Usage

Register the NetSync services in your application startup:

```csharp
// register NetSync services
services.AddNetSync();
```

Different options can be set:

### Manual mode
If you want to start the NetSync service manually, you can set the `ManualStart` option to `true`. This will prevent the service from starting automatically when the application starts. You will need to start the service manually by calling the `Start` method on the registered `NetSyncOptions`.
```csharp
services.AddNetSync(options =>
{
    options.ManualStart = true;
}
```

## Protocols to keep data in sync
- Introduction
  - The discovery module will announce the presence of a client on the network. The other clients will receive this announcement and add the client to their pool of clients.
  - Greeting -> Handshake -> Pool Initiative -> Highest roller updates the new client.
- Update distribution
  - A client will always send its data to all other clients in the pool.
- Gossip propagation
  - At random times, a client will check with a random connection to see if it has the latest data.
- Vector Clocks
  - Every client has a vector clock that is incremented when it receives data. Every time a client sends data, it includes its vector clock. The receiving client will update its vector clock to the maximum of the two clocks. The receiving client can determine if it is missing updates from certain other clients in the pool.
- Versioning (Timestamping)
  - Each data item has a version number. When a client receives data, it checks the version number. If the version number is higher than the current version number, the client updates its data. If the version number is lower, the client ignores the data.
- Keepalive
  - Each client sends a keep-alive message to all other clients in the pool at regular intervals. If a client does not receive a keep-alive message from another client within a certain time period, it assumes that the client is no longer available and removes it from the pool.
  - When a client reintroduces itself to the pool, an introduction handshake and vectorclock is exchanged.

### sequene diagram
```mermaid
sequenceDiagram
    %% Introduction/Discovery: One client
    rect rgb(245,245,245)
        participant A as Client A (new)
        Note over A: Alone on network, waits for announcements
    end

    %% Introduction/Discovery: Two clients (A joins B)
    rect rgb(230,245,255)
        participant B as Client B (existing)
        A->>B: Discovery Announce (UDP)
        B->>A: Discovery Response (TCP)
        A->>B: Greeting
        B->>A: Handshake
        Note over A,B: Both add each other to pool
    end

    %% Introduction/Discovery: Three or more clients (A joins B and C)
    rect rgb(220,255,220)
        participant C as Client C (existing)
        A->>B: Discovery Announce (UDP)
        A->>C: Discovery Announce (UDP)
        B->>A: Discovery Response (TCP)
        C->>A: Discovery Response (TCP)
        A->>B: Greeting
        A->>C: Greeting
        B->>A: Handshake
        C->>A: Handshake
        Note over B,C: Pool Initiative (Highest roller updates new client)
        B->>A: Send Full Data (Update)
    end

    %% Update Distribution
    rect rgb(255,245,230)
        A->>B: Data Update (Broadcast)
        A->>C: Data Update (Broadcast)
    end

    %% Gossip Propagation
    rect rgb(255,255,220)
        B->>C: Gossip Sync Request (Random interval)
        C->>B: Gossip Sync Response (Latest Data)
    end

    %% Vector Clocks
    rect rgb(245,230,255)
        A->>B: Data Update [Vector Clock]
        B->>A: Data Update [Vector Clock]
        Note over A,B: Each update includes vector clock for conflict resolution
    end

    %% Versioning
    rect rgb(230,255,255)
        B->>C: Data Update [Version]
        C->>B: Accepts if version is newer, ignores if older
    end

    %% Keepalive
    rect rgb(255,230,230)
        A->>B: Keepalive Ping (Interval)
        B->>A: Keepalive Pong
        Note over A,B: If no keepalive, client is removed from pool
    end

    %% Reintroduction
    rect rgb(240,240,240)
        A->>B: Discovery Announce (Rejoin)
        B->>A: Handshake + Vector Clock Exchange
    end
```
