
# llm_semantic_search_test

## Purpose
This repository contains a compact, reproducible set of experiments to validate semantic retrieval using a Redis vector store and to observe how generation parameters (temperature, top_p, max_tokens) affect model output. The materials are presented as test assets and a concise experiment plan intended for reproducible validation and demonstration of engineering quality.

## Repository layout (suggested)


llm_semantic_search_test/
├── ChatClient.cs // .NET wrapper for chat calls (minimal, production-minded)
├── README.md // This file
└── data/
├── documents.json // Document corpus (D1–D5)
└── queries.json // Query set (Q1–Q5)


## Document corpus
Use the following five documents as the indexed corpus. They are intentionally short to make similarity behavior clear.

**documents.json**
```json
[
  { "id": "D1", "text": "Last night the bright moon hung low above the city, casting silver light across the rooftops." },
  { "id": "D2", "text": "I spent the afternoon tuning my dirt-bike’s suspension for a rocky trail ride next weekend." },
  { "id": "D3", "text": "A simple tomato and basil salad is fresh and quick to prepare after a long day." },
  { "id": "D4", "text": "Scientists warn that coastal cities are seeing more frequent flooding as sea levels rise." },
  { "id": "D5", "text": "The local football team practiced set plays until the sun dipped behind the stadium." }
]

Query set

Use these queries to validate retrieval behavior and measure similarity scores.

queries.json

[
  { "id": "Q1", "query": "silver moonlight over houses" },
  { "id": "Q2", "query": "off-road bike suspension problems" },
  { "id": "Q3", "query": "easy dinner with tomatoes and basil" },
  { "id": "Q4", "query": "rising seas cause floods in coastal towns" },
  { "id": "Q5", "query": "concert lights and cheering crowd" }
]

Expected retrieval matches
Query	Expected top-1	Rationale
Q1	D1	Semantic match on moon / rooftops / silver light
Q2	D2	Motorcycle / dirt-bike / suspension semantics
Q3	D3	Cooking / tomato / basil / quick meal
Q4	D4	Sea-level rise / coastal flooding semantics
Q5	D5 (weak)	Stadium / crowd context; used as a noise test for false positives
Retrieval evaluation procedure (concise)

Generate embeddings for every document using the same embedding model and insert into Redis (or equivalent vector store).

For each query, compute its embedding and perform a top-3 similarity search.

Record top-3 document ids and similarity scores.

Validate whether top-1 equals expected match; record confidence per thresholds below.

Confidence thresholds:

High ≥ 0.80

Medium 0.65–0.79

Low < 0.65

Notes:

Use the same embedding model for corpus and queries.

Normalize vectors consistently if using cosine similarity.

Generation experiments (post-retrieval)

After retrieving the best-matching document, run LLM generation experiments to observe how parameters affect output and token usage.

Base prompt

System: You are a concise assistant.
User: Summarize the following document in one paragraph:
[DOCUMENT_TEXT]


Experiment matrix

Label	temperature	top_p	max_tokens	Expected behavior
A	0.0	0.1	60	Deterministic, concise summary, reproducible
B	0.6	0.9	150	Balanced phrasing, moderate length, mild variation
C	1.0	0.95	350	Creative/extended output, higher variation and token cost

Record for each run:

Retrieval: query id, retrieved doc id(s), similarity scores (top-3).

Generation: parameter set (temperature/top_p/max_tokens), returned text, token usage (input / output / total).

A short judgment: matches retrieved doc (yes/partial/no) and notes on hallucination or omissions.

Data collection template

Use a CSV or table with these columns for reproducibility and inspection:

Query	RetrievedDoc	SimScore	Temp	Top_p	MaxTokens	InputTokens	OutputTokens	TotalTokens	Notes

Example row:
| Q1 | D1 | 0.82 | 0.0 | 0.1 | 60 | 148 | 57 | 205 | Deterministic, factual |

Implementation notes (concise)

Retrieval is embedding + similarity. Temperature/top_p/max_tokens do not affect embedding similarity. They only affect the generation stage after retrieval.

To minimize hallucination, include an instruction in the prompt such as: "Only use the text below to answer. If the requested information is not present, reply 'insufficient information'."

Log and persist both embedding insertion and query results to enable reproducible analysis.

Token accounting and cost

Track per-call token counts provided by the LLM API (input/prompt, output/completion, total).

Approximation: 1 token ≈ 4 characters (English) — use official tokenizer where possible for precise accounting.

Maintain a short-run cost estimate by multiplying token totals by the provider's cost per 1k tokens.

Console logging example (C#-style)

// usage: object returned by the LLM SDK with token counts
Console.WriteLine($"PromptTokens: {usage.PromptTokens}, CompletionTokens: {usage.CompletionTokens}, TotalTokens: {usage.TotalTokens}");

Validation checklist

 All documents inserted with embeddings and verified in Redis.

 Queries produce expected top-1 matches at medium/high confidence for Q1–Q4.

 Q5 demonstrates low-confidence/ambiguous match (noise test).

 Generation experiments executed for each query/document pair and logged.

 Notes captured on how parameter changes affected length, style, and token cost.

 Rate-limit handling validated (e.g., backoff+retry after 429).

Observations (fill in after runs)

Provide concise bullet findings here after running tests. Example fields to populate:

Retrieval accuracy summary (per-query similarity stats).

Token consumption comparison across experiments A/B/C.

Examples of deterministic vs creative outputs and any hallucination cases.

Any issues (embedding mismatches, indexing errors, rate-limit incidents).

How this artifact is intended to be read

This repository documents a reproducible validation harness and experiments; it is written for a technical reviewer or peer. The primary signal is demonstrable evidence of reliability in retrieval, disciplined parameter experimentation, and clear token/cost accounting.

Minimal troubleshooting pointers

If retrieval returns unexpected high-similarity results: confirm identical embedding model for documents and queries, check vector normalization, verify document insertion succeeded.

If generation hallucinates despite retrieved context: tighten prompt instructions, reduce temperature/top_p, or reduce the retrieval-to-generation context size.
