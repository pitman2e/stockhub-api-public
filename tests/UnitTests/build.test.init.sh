#!/bin/sh
dotnet test --no-restore --logger "trx;LogFileName=test_results.trx"
