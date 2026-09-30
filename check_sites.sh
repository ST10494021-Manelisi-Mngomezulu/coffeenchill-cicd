#!/bin/bash
# Loops through a list of websites and checks if each one is reachable using curl

websites=(
    "https://www.google.com"
    "https://www.github.com"
    "https://hub.docker.com"
    "https://mystudies.iie.edu.za"
    "https://thissitedoesnotexist12345.com"
)

for site in "${websites[@]}"; do
    status=$(curl -s -o /dev/null -L --max-time 10 -w "%{http_code}" "$site")
    if [[ "$status" =~ ^[23] ]]; then
        echo "$site is REACHABLE (HTTP $status)"
    else
        echo "$site is NOT reachable (HTTP $status)"
    fi
done