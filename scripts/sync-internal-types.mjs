/**
 * Generates TypeScript interfaces from C# DTO classes and writes them to the frontend repo.
 *
 * Reads specific C# source files from PreflightApi.Infrastructure/Dtos, parses their
 * property definitions, and outputs equivalent TypeScript interfaces.
 *
 * Usage:
 *   node scripts/sync-internal-types.mjs
 *   node scripts/sync-internal-types.mjs --out <custom-output-path>
 *
 * Output: ../preflightapi.frontend/src/generated/internal-api.ts (default)
 */

import { readFileSync, writeFileSync, existsSync } from 'node:fs'
import { resolve, dirname } from 'node:path'
import { fileURLToPath } from 'node:url'

const __dirname = dirname(fileURLToPath(import.meta.url))
const REPO_ROOT = resolve(__dirname, '..')
const DTO_DIR = resolve(REPO_ROOT, 'PreflightApi.Infrastructure', 'Dtos')

const DEFAULT_OUTPUT = resolve(
  REPO_ROOT,
  '..',
  'preflightapi.frontend',
  'src',
  'generated',
  'internal-api.ts',
)

// C# files to process (order matters — dependencies first)
const SOURCE_FILES = [
  'DataCurrencyResult.cs',
  'DataCurrencySummary.cs', // contains DataCurrencySummary + DataCurrencyResponse
  'HealthCheckResult.cs', // contains HealthCheckEntry + HealthCheckResponse
]

// C# type → TypeScript type mapping
const TYPE_MAP = {
  string: 'string',
  bool: 'boolean',
  int: 'number',
  long: 'number',
  float: 'number',
  double: 'number',
  decimal: 'number',
  DateTime: 'string',
}

function parseArgs() {
  const args = process.argv.slice(2)
  const outIndex = args.indexOf('--out')
  if (outIndex !== -1 && args[outIndex + 1]) {
    return resolve(args[outIndex + 1])
  }
  return DEFAULT_OUTPUT
}

/**
 * Maps a C# type string to a TypeScript type string.
 */
function mapType(csType) {
  // Nullable value types: int?, double?, DateTime?, bool?
  const nullableMatch = csType.match(/^(\w+)\?$/)
  if (nullableMatch) {
    const inner = TYPE_MAP[nullableMatch[1]]
    if (inner) return `${inner} | null`
  }

  // Direct primitive mapping
  if (TYPE_MAP[csType]) return TYPE_MAP[csType]

  // Nullable reference types: string?
  if (csType === 'string?') return 'string | null'

  // Dictionary<string, int> → Record<string, number>
  const dictMatch = csType.match(/^Dictionary<(\w+),\s*(\w+)>$/)
  if (dictMatch) {
    const keyType = TYPE_MAP[dictMatch[1]] || dictMatch[1]
    const valType = TYPE_MAP[dictMatch[2]] || dictMatch[2]
    return `Record<${keyType}, ${valType}>`
  }

  // IReadOnlyList<T>, List<T>, IEnumerable<T> → Array<T>
  const listMatch = csType.match(
    /^(?:IReadOnlyList|List|IEnumerable|ICollection)<(\w+)>$/,
  )
  if (listMatch) {
    const inner = TYPE_MAP[listMatch[1]] || listMatch[1]
    return `Array<${inner}>`
  }

  // Fallback — use the type name as-is (for references to other DTOs)
  return csType
}

/**
 * Converts a PascalCase C# property name to camelCase for JSON serialization.
 * (System.Text.Json default camelCase policy)
 */
function toCamelCase(name) {
  return name.charAt(0).toLowerCase() + name.slice(1)
}

/**
 * Parses a single C# file and extracts all class definitions with their properties.
 * Returns an array of { className, properties: [{ name, tsType }] }
 */
function parseCsFile(filePath) {
  const content = readFileSync(filePath, 'utf-8')
  const classes = []

  // Match class declarations — handles "public class Foo" and "public class Foo : Bar"
  const classRegex = /public\s+class\s+(\w+)(?:\s*:\s*\w+)?\s*\{/g
  let classMatch

  while ((classMatch = classRegex.exec(content)) !== null) {
    const className = classMatch[1]
    const classStart = classMatch.index + classMatch[0].length

    // Find matching closing brace (simple depth tracking)
    let depth = 1
    let pos = classStart
    while (depth > 0 && pos < content.length) {
      if (content[pos] === '{') depth++
      if (content[pos] === '}') depth--
      pos++
    }
    const classBody = content.slice(classStart, pos - 1)

    // Match properties: public Type Name { get; ... }
    // Handles: required, nullable (?), generics (List<T>, Dictionary<K,V>)
    const propRegex =
      /public\s+(?:required\s+)?(\S+(?:<[^>]+>)?)\??\s+(\w+)\s*\{[^}]*get;/g

    // More precise regex that captures the full type including nullable marker
    const preciseRegex =
      /public\s+(?:required\s+)?([\w]+(?:<[\w\s,]+>)?\??)\s+(\w+)\s*\{[^}]*get;/g

    const properties = []
    let propMatch

    while ((propMatch = preciseRegex.exec(classBody)) !== null) {
      const csType = propMatch[1].trim()
      const propName = propMatch[2]
      const tsType = mapType(csType)

      properties.push({
        name: toCamelCase(propName),
        tsType,
      })
    }

    classes.push({ className, properties })
  }

  return classes
}

function generate() {
  const outputPath = parseArgs()

  console.log('Syncing internal API types from C# DTOs...\n')

  const allClasses = []

  for (const file of SOURCE_FILES) {
    const filePath = resolve(DTO_DIR, file)
    if (!existsSync(filePath)) {
      console.warn(`  SKIP  ${file} (not found)`)
      continue
    }

    const classes = parseCsFile(filePath)
    for (const cls of classes) {
      console.log(`  PARSE ${file} → ${cls.className} (${cls.properties.length} properties)`)
    }
    allClasses.push(...classes)
  }

  if (allClasses.length === 0) {
    console.error('\nNo classes found. Check SOURCE_FILES paths.')
    process.exit(1)
  }

  // Generate TypeScript
  const lines = [
    '/**',
    ' * Auto-generated from C# DTOs in PreflightApi.Infrastructure/Dtos.',
    ' * Do not edit manually — run `node scripts/sync-internal-types.mjs` from the backend repo.',
    ' */',
    '',
  ]

  for (const cls of allClasses) {
    lines.push(`export interface ${cls.className} {`)
    for (const prop of cls.properties) {
      lines.push(`  ${prop.name}: ${prop.tsType}`)
    }
    lines.push('}')
    lines.push('')
  }

  const output = lines.join('\n')

  // Verify output directory exists
  const outputDir = dirname(outputPath)
  if (!existsSync(outputDir)) {
    console.error(`\nOutput directory does not exist: ${outputDir}`)
    console.error('Make sure the frontend repo is cloned as a sibling directory.')
    process.exit(1)
  }

  writeFileSync(outputPath, output)
  console.log(`\nWrote ${allClasses.length} interfaces to:\n  ${outputPath}`)
}

generate()
