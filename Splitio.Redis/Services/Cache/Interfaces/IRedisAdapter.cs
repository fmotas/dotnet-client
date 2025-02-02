﻿using StackExchange.Redis;
using System;
using System.Collections.Generic;

namespace Splitio.Redis.Services.Cache.Interfaces
{
    public interface IRedisAdapter
    {
        bool Set(string key, string value);

        string Get(string key);

        RedisValue[] MGet(RedisKey[] keys);

        RedisKey[] Keys(string pattern);

        bool Del(string key);

        long Del(RedisKey[] keys);

        bool SAdd(string key, RedisValue value);

        long SAdd(string key, RedisValue[] values);

        long SRem(string key, RedisValue[] values);

        long ListRightPush(string key, RedisValue value);

        long ListRightPush(string key, RedisValue[] values);

        bool SIsMember(string key, string value);

        RedisValue[] SMembers(string key);

        long IcrBy(string key, long delta);

        void Flush();

        bool IsConnected();

        bool KeyExpire(string key, TimeSpan expiry);

        RedisValue[] ListRange(RedisKey key, long start = 0, long stop = -1);

        void Connect();

        double HashIncrement(string key, string field, double value);

        bool HashSet(RedisKey key, RedisValue hashField, RedisValue value);

        long HashIncrementAsyncBatch(string key, Dictionary<string, int> values);

        HashEntry[] HashGetAll(RedisKey key);
    }
}
