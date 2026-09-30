#!/bin/bash
# Loops 1-20: Fizz (div by 3), Buzz (div by 5), FizzBuzz (both), else the number

for i in $(seq 1 20); do
    if (( i % 15 == 0 )); then
        echo "FizzBuzz"
    elif (( i % 3 == 0 )); then
        echo "Fizz"
    elif (( i % 5 == 0 )); then
        echo "Buzz"
    else
        echo "$i"
    fi
done