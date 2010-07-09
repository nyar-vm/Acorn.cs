namespace Acorn.Redis.Data;

/// <summary>
///     Redis 协议常量。
/// </summary>
public static class RedisConstants
{
    /// <summary>
    ///     简单字符串前缀。
    /// </summary>
    public const char SimpleStringPrefix = '+';
    
    /// <summary>
    ///     错误前缀。
    /// </summary>
    public const char ErrorPrefix = '-';
    
    /// <summary>
    ///     整数前缀。
    /// </summary>
    public const char IntegerPrefix = ':';
    
    /// <summary>
    ///     批量字符串前缀。
    /// </summary>
    public const char BulkStringPrefix = '$';
    
    /// <summary>
    ///     数组前缀。
    /// </summary>
    public const char ArrayPrefix = '*';
    
    /// <summary>
    ///     回车符。
    /// </summary>
    public const char CarriageReturn = '\r';
    
    /// <summary>
    ///     换行符。
    /// </summary>
    public const char LineFeed = '\n';
    
    /// <summary>
    ///     空批量字符串。
    /// </summary>
    public const int NullBulkString = -1;
    
    /// <summary>
    ///     空数组。
    /// </summary>
    public const int NullArray = -1;
    
    /// <summary>
    ///     常用命令。
    /// </summary>
    public static class Commands
    {
        /// <summary>
        ///     PING 命令。
        /// </summary>
        public const string Ping = "PING";
        /// <summary>
        ///     SET 命令。
        /// </summary>
        public const string Set = "SET";
        /// <summary>
        ///     GET 命令。
        /// </summary>
        public const string Get = "GET";
        /// <summary>
        ///     DEL 命令。
        /// </summary>
        public const string Del = "DEL";
        /// <summary>
        ///     EXISTS 命令。
        /// </summary>
        public const string Exists = "EXISTS";
        /// <summary>
        ///     INCR 命令。
        /// </summary>
        public const string Incr = "INCR";
        /// <summary>
        ///     DECR 命令。
        /// </summary>
        public const string Decr = "DECR";
        /// <summary>
        ///     HSET 命令。
        /// </summary>
        public const string HSet = "HSET";
        /// <summary>
        ///     HGET 命令。
        /// </summary>
        public const string HGet = "HGET";
        /// <summary>
        ///     HGETALL 命令。
        /// </summary>
        public const string HGetAll = "HGETALL";
        /// <summary>
        ///     LPUSH 命令。
        /// </summary>
        public const string LPush = "LPUSH";
        /// <summary>
        ///     RPUSH 命令。
        /// </summary>
        public const string RPush = "RPUSH";
        /// <summary>
        ///     LPOP 命令。
        /// </summary>
        public const string LPop = "LPOP";
        /// <summary>
        ///     RPOP 命令。
        /// </summary>
        public const string RPop = "RPOP";
        /// <summary>
        ///     LLEN 命令。
        /// </summary>
        public const string LLen = "LLEN";
        /// <summary>
        ///     SADD 命令。
        /// </summary>
        public const string SAdd = "SADD";
        /// <summary>
        ///     SREM 命令。
        /// </summary>
        public const string SRem = "SREM";
        /// <summary>
        ///     SMEMBERS 命令。
        /// </summary>
        public const string SMembers = "SMEMBERS";
        /// <summary>
        ///     SCARD 命令。
        /// </summary>
        public const string SCard = "SCARD";
        /// <summary>
        ///     ZADD 命令。
        /// </summary>
        public const string ZAdd = "ZADD";
        /// <summary>
        ///     ZREM 命令。
        /// </summary>
        public const string ZRem = "ZREM";
        /// <summary>
        ///     ZRANGE 命令。
        /// </summary>
        public const string ZRange = "ZRANGE";
        /// <summary>
        ///     ZCARD 命令。
        /// </summary>
        public const string ZCard = "ZCARD";
        /// <summary>
        ///     EXPIRE 命令。
        /// </summary>
        public const string Expire = "EXPIRE";
        /// <summary>
        ///     TTL 命令。
        /// </summary>
        public const string Ttl = "TTL";
        /// <summary>
        ///     PTTL 命令。
        /// </summary>
        public const string PTtl = "PTTL";
        /// <summary>
        ///     KEYS 命令。
        /// </summary>
        public const string Keys = "KEYS";
        /// <summary>
        ///     FLUSHDB 命令。
        /// </summary>
        public const string FlushDb = "FLUSHDB";
        /// <summary>
        ///     FLUSHALL 命令。
        /// </summary>
        public const string FlushAll = "FLUSHALL";
        /// <summary>
        ///     AUTH 命令。
        /// </summary>
        public const string Auth = "AUTH";
        /// <summary>
        ///     SELECT 命令。
        /// </summary>
        public const string Select = "SELECT";
        /// <summary>
        ///     INFO 命令。
        /// </summary>
        public const string Info = "INFO";
        /// <summary>
        ///     CONFIG 命令。
        /// </summary>
        public const string Config = "CONFIG";
        /// <summary>
        ///     CLIENT 命令。
        /// </summary>
        public const string Client = "CLIENT";
        /// <summary>
        ///     MONITOR 命令。
        /// </summary>
        public const string Monitor = "MONITOR";
        /// <summary>
        ///     SUBSCRIBE 命令。
        /// </summary>
        public const string Subscribe = "SUBSCRIBE";
        /// <summary>
        ///     UNSUBSCRIBE 命令。
        /// </summary>
        public const string Unsubscribe = "UNSUBSCRIBE";
        /// <summary>
        ///     PUBLISH 命令。
        /// </summary>
        public const string Publish = "PUBLISH";
    }
}