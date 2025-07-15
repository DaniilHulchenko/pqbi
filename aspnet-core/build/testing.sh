#!/bin/bash

source containerRunner.sh

config=$1
group=$2

# App "2__multi_tenant" "sanity_tests"
# App "$config" "$group"
App "$config" "$group" "" "testing"

printf "The end."
 