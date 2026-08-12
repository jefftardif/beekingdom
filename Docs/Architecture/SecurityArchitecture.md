# Bee Kingdom Security Architecture

## Authentication Boundary

All future player access must pass through `BeeKingdom.Authentication`. Gateways and gameplay services should validate tokens before accepting requests.

## Token Model

Bee Kingdom uses opaque access and refresh tokens:

* access tokens are short-lived;
* refresh tokens are longer-lived;
* refresh tokens rotate on use;
* revoked tokens are rejected;
* expired tokens are rejected.

## Session Model

Sessions include:

* `SessionId`
* `PlayerId`
* `AccountId`
* authentication provider;
* login time;
* last activity;
* expiration;
* client version;
* IP address;
* device identifier;
* region;
* revocation state.

## Protection Controls

Initial controls:

* PBKDF2 password hashing;
* opaque random token generation;
* hashed token storage;
* configurable max attempts;
* temporary account lockout;
* configurable max sessions per account;
* global session logout.

Future controls:

* SQL-backed credential store;
* external identity providers;
* device trust scoring;
* anomaly detection;
* gateway-level rate limiting;
* signed audit trail.
