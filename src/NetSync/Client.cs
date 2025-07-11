using System.Net;

namespace NetSync;

public record Client(string Id, IPEndPoint Endpoint);