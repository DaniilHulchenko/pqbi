#!/bin/bash

source containerRunner.sh

config=$1
pqsServiceUrl=$2

# App "2__multi_tenant" "sanity_tests"
App "$config" "" "$pqsServiceUrl" "Staging"

printf "The end."
 