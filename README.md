# Multi-turn Gen AI Chat Bot On Complex Documents  

Today, we are going to explore how to utilize the power of gen ai to help answer questions on complex documents.  In this example, we will use publicly available SEC filings to demonstrate how to build a multi-turn chatbot that can answer questions on complex purchase agreements.  We will use the OpenAI GPT4-o model to build the chatbot.  The chatbot will be able to answer questions on the document and provide additional context to the user.  We will also explore how to handle multi-turn conversations and maintain context across multiple interactions.

## Table of Contents
1. [Introduction](#introduction)
2. [Architecture](#architecture)
3. [Data](#data)
4. [Setup](#setup)

## Introduction
Getting started with ai chatbot is easy with Azure AI Studio.  However, when the data is complex, such as legal filing, it can be challenging to build a chatbot that can understand the context and provide accurate answers with just prompting techniques alone.  AI developer can run into real challenges when words means one thing to humam but means something else in the context of the document.  Another major challenge with [RAG](https://learn.microsoft.com/en-us/azure/search/retrieval-augmented-generation-overview) implementation is that the retreival system might find the same term in 20 different documents and the model might not be able to distinguish between them.  This is where the power of multi-turn chatbot comes in.  The multi-turn chatbot can maintain context across multiple interactions and provide more accurate answers to the user.  In this example, we will build a multi-turn chatbot that can answer questions on complex purchase agreements.

## Architecture
The architecture follows exactly as described in [gen-ai-in-a-box](https://github.com/Azure-Samples/gen-ai-bot-in-a-box/tree/main).  We utilze the Semantic Kernal in dotnet c# as a reference closely.

## Data
We will use publicly available SEC filings to demonstrate how to build a multi-turn chatbot that can answer questions on complex purchase agreements.  The data is available on the SEC website and can be downloaded in pdf format.  With a ai bot trying to reason over multiple of these SEC filings, we have to make sure the resul is well-grounded and accurate.  

Take this [Deopomed filing](https://www.sec.gov/Archives/edgar/data/1005201/000110465906080867/a06-25523_1ex10d1.htm) as example.  If you are looking for deal amount or purchas price, you won't find it searching by those terms.  
