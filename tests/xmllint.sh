#!/bin/sh
# Validates every example against the schema named in its
# xsi:noNamespaceSchemaLocation, using libxml2's xmllint.
#
# Run from the repository root:
#   sh tests/xmllint.sh

failures=0

for xml in $(find . -path '*/examples/*.xml' | sort); do
    location=$(grep -o 'noNamespaceSchemaLocation="[^"]*"' "$xml" | head -n 1 | cut -d '"' -f 2)

    if [ -z "$location" ]; then
        echo "FAIL $xml"
        echo "  missing xsi:noNamespaceSchemaLocation"
        failures=$((failures + 1))
        continue
    fi

    xsd="$(dirname "$xml")/$location"

    if output=$(xmllint --noout --nonet --schema "$xsd" "$xml" 2>&1); then
        echo "ok   $xml"
    else
        echo "FAIL $xml"
        echo "$output" | sed 's/^/  /'
        failures=$((failures + 1))
    fi
done

echo
if [ "$failures" -eq 0 ]; then
    echo "All passed."
else
    echo "$failures file(s) failed."
    exit 1
fi
