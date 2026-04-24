# 📦 Acorn.PostgreSQL

PostgreSQL 前端/后端通信协议（FE/BE 协议）编解码器。

## 📐 格式布局

### PostgreSQL 消息头

| 字段 | 偏移 | 大小 | 说明 | 对应类 |
|---|---|---|---|---|
| Type | 0x00 | 1 | 消息类型标识符（ASCII 字符） | `PostgreSqlMessageData.Type` |
| Length | 0x01 | 4 | 消息长度（含自身，大端序） | `PostgreSqlMessageData.Length` |
| Payload | 0x05 | N | 消息体数据 | `PostgreSqlMessageData.Data` |

### 常见消息类型

| 类型字符 | 名称 | 说明 | 方向 |
|---|---|---|---|
| 'R' | AuthenticationRequest | 认证请求 | 服务器→客户端 |
| 'p' | PasswordMessage | 密码消息 | 客户端→服务器 |
| 'Q' | Query | 简单查询 | 客户端→服务器 |
| 'T' | RowDescription | 行描述 | 服务器→客户端 |
| 'D' | DataRow | 数据行 | 服务器→客户端 |
| 'C' | CommandComplete | 命令完成 | 服务器→客户端 |
| 'E' | ErrorResponse | 错误响应 | 服务器→客户端 |
| 'S' | ParameterStatus | 参数状态 | 服务器→客户端 |
| 'Z' | ReadyForQuery | 就绪查询 | 服务器→客户端 |
| '1' | ParseComplete | 解析完成 | 服务器→客户端 |
| '2' | BindComplete | 绑定完成 | 服务器→客户端 |
| '3' | CloseComplete | 关闭完成 | 服务器→客户端 |
| 'n' | NoData | 无数据 | 服务器→客户端 |
| 't' | ParameterDescription | 参数描述 | 服务器→客户端 |
| 'I' | EmptyQueryResponse | 空查询响应 | 服务器→客户端 |

### 认证请求（Authentication Request）

| 字段 | 大小 | 说明 | 对应类 |
|---|---|---|---|
| AuthType | 4 | 认证类型 | `PostgreSqlConstants.AuthenticationType` |
| AuthData | 变长 | 认证数据 | `PostgreSqlMessageData.AuthenticationData` |

### 常见认证类型

| 值 | 名称 | 说明 |
|---|---|---|
| 0 | Ok | 认证成功 |
| 3 | CleartextPassword | 明文密码 |
| 5 | MD5Password | MD5 密码 |
| 10 | SASL | SASL 认证 |
| 11 | SASLContinue | SASL 继续 |
| 12 | SASLFinal | SASL 最终 |

## 🏗️ 核心类

| 类 | 说明 | 文件 |
|---|---|---|
| `PostgreSqlMessageData` | PostgreSQL 消息 | [Data/PostgreSqlMessageData.cs](Data/PostgreSqlMessageData.cs) |
| `PostgreSqlConstants` | PostgreSQL 协议常量 | [Data/PostgreSqlConstants.cs](Data/PostgreSqlConstants.cs) |
| `PostgreSqlFieldDescription` | 字段描述 | [Data/PostgreSqlMessageData.cs](Data/PostgreSqlMessageData.cs) |
| `PostgreSqlDecoder` | PostgreSQL 协议解码器 | [Decode/PostgreSqlDecoder.cs](Decode/PostgreSqlDecoder.cs) |
| `PostgreSqlEncoder` | PostgreSQL 协议编码器 | [Encode/PostgreSqlEncoder.cs](Encode/PostgreSqlEncoder.cs) |
| `PostgreSqlScanner` | PostgreSQL 协议扫描器 | [Scanner/PostgreSqlScanner.cs](Scanner/PostgreSqlScanner.cs) |

## 📚 格式规范参考

- [PostgreSQL Frontend/Backend Protocol](https://www.postgresql.org/docs/current/protocol.html)
- [PostgreSQL Message Formats](https://www.postgresql.org/docs/current/protocol-message-formats.html)
- [PostgreSQL Message Flow](https://www.postgresql.org/docs/current/protocol-flow.html)
