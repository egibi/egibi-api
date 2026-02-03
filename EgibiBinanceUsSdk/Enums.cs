namespace EgibiBinanceUsSdk
{
    public enum SymbolStatuses
    {
        PRE_TRADING,
        TRADING,
        POST_TRADING,
        END_OF_DAY,
        HALT,
        AUCTION_MATCH,
        BREAK
    }

    public enum SymbolTypes
    {
        SPOT
    }

    public enum OrderStatuses
    {
        NEW,
        PARTIALLY_FILLED,
        FILLED,
        CANCELED,
        PENDING_CANCEL,
        REJECTED,
        EXPIRED,
        EXPIRED_IN_MATCH
    }

    public enum OcoStatuses
    {
        RESPONSE,
        EXEC_STARTED,
        ALL_DONE
    }

    public enum OcoOrderStatuses
    {
        EXECUTING,
        ALL_DONE,
        REJECT
    }

    public enum ContingencyTypes
    {
        OCO
    }

    public enum OrderTypes
    {
        LIMIT,
        MARKET,
        STOP_LOSS,
        STOP_LOSS_LIMIT,
        TAKE_PROFIT,
        TAKE_PROFIT_LIMIT,
        LIMIT_MAKER
    }

    public enum OrderSides
    {
        BUY,
        SELL
    }

    public enum TimeInForces
    {
        GTC,
        IOC,
        FOK
    }

    public enum RateLimitTypes
    {
        REQUEST_WEIGHT,
        ORDERS,
        RAW_REQUESTS
    }

    public enum RateLimitIntervals
    {
        SECOND,
        MINUTE,
        DAY
    }

    public static class KlineIntervals
    {
        public static string _1m => "1m";
        public static string _3m => "3m";
        public static string _5m => "5m";
        public static string _15m => "15m";
        public static string _30m => "30m";
        public static string _1h => "1h";
        public static string _2h => "2h";
        public static string _4h => "4h";
        public static string _6h => "6h";
        public static string _8h => "8h";
        public static string _12h => "12h";
        public static string _1d => "1d";
        public static string _3d => "3d";
        public static string _1w => "1w";
        public static string _1M => "1M";
    }

    public enum DataSources
    {
        MATCHING_ENGINE,
        MEMORY,
        DATABASE
    }

    public enum OrderResponseTypes
    {
        ACK,
        RESULT,
        FULL
    }
}
