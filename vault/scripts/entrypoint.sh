#!/bin/sh
set -e

chown -R 100:1000 /vault/data /vault/logs

exec su-exec vault vault server -config=/vault/config/vault.hcl
