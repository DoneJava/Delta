namespace DELTAAPI.Model
{
    public class RetornoDTO
    {
        public bool Sucesso { get; set; }
        public string? Mensagem { get; set; }
        public dynamic? Objeto { get; set; }
        public StatusRetorno Status { get; set; }
    }

    public enum StatusRetorno
    {
        // 1xx Informational
        Continue = 100,
        SwitchingProtocols = 101,
        Processing = 102,              // WebDAV
        EarlyHints = 103,              // HTTP/1.1

        // 2xx Success
        OK = 200,
        Created = 201,
        Accepted = 202,
        NonAuthoritativeInformation = 203,
        NoContent = 204,
        ResetContent = 205,
        PartialContent = 206,
        MultiStatus = 207,             // WebDAV
        AlreadyReported = 208,         // WebDAV
        IMUsed = 226,                  // HTTP Delta encoding

        // 3xx Redirection
        MultipleChoices = 300,
        MovedPermanently = 301,
        Found = 302,
        SeeOther = 303,
        NotModified = 304,
        UseProxy = 305,
        TemporaryRedirect = 307,
        PermanentRedirect = 308,

        // 4xx Client Errors
        BadRequest = 400,
        Unauthorized = 401,
        PaymentRequired = 402,
        Forbidden = 403,
        NotFound = 404,
        MethodNotAllowed = 405,
        NotAcceptable = 406,
        ProxyAuthenticationRequired = 407,
        RequestTimeout = 408,
        Conflict = 409,
        Gone = 410,
        LengthRequired = 411,
        PreconditionFailed = 412,
        PayloadTooLarge = 413,
        URITooLong = 414,
        UnsupportedMediaType = 415,
        RangeNotSatisfiable = 416,
        ExpectationFailed = 417,
        ImATeapot = 418,                  // RFC 2324
        MisdirectedRequest = 421,
        UnprocessableEntity = 422,        // WebDAV
        Locked = 423,                     // WebDAV
        FailedDependency = 424,           // WebDAV
        TooEarly = 425,
        UpgradeRequired = 426,
        PreconditionRequired = 428,
        TooManyRequests = 429,
        RequestHeaderFieldsTooLarge = 431,
        UnavailableForLegalReasons = 451,

        // 5xx Server Errors
        InternalServerError = 500,
        NotImplemented = 501,
        BadGateway = 502,
        ServiceUnavailable = 503,
        GatewayTimeout = 504,
        HTTPVersionNotSupported = 505,
        VariantAlsoNegotiates = 506,
        InsufficientStorage = 507,        // WebDAV
        LoopDetected = 508,               // WebDAV
        NotExtended = 510,
        NetworkAuthenticationRequired = 511
    }

}
