# Multi-turn Gen AI Chat Bot On Complex Documents  

Today, we are going to explore how to utilize the power of gen ai to help answer questions on complex documents.  In this example, we will use publicly available SEC filings to demonstrate how to build a multi-turn chatbot that can answer questions on complex purchase agreements.  We will use the OpenAI GPT4-o model to build the chatbot.  The chatbot will be able to answer questions on the document and provide additional context to the user.  We will also explore how to handle multi-turn conversations and maintain context across multiple interactions.

## Table of Contents
1. [Introduction](#introduction)
2. [Architecture](#architecture)
3. [Data](#data)
4. [Setup](#setup)
5. [Another Approach](#another-approach)
6. [The Code](#the-code)


## Introduction
Getting started with ai chatbot is easy with Azure AI Studio.  However, when the data is complex, such as legal filing, it can be challenging to build a chatbot that can understand the context and provide accurate answers with just prompting techniques alone.  AI developer can run into real challenges when words means one thing to humam but means something else in the context of the document.  Another major challenge with [RAG](https://learn.microsoft.com/en-us/azure/search/retrieval-augmented-generation-overview) implementation is that the retreival system might find the same term in 20 different documents and the model might not be able to distinguish between them.  This is where the power of multi-turn chatbot comes in.  The multi-turn chatbot can maintain context across multiple interactions and provide more accurate answers to the user.  In this example, we will build a multi-turn chatbot that can answer questions on complex purchase agreements.

## Architecture
The architecture follows exactly as described in [gen-ai-in-a-box](https://github.com/Azure-Samples/gen-ai-bot-in-a-box/tree/main).  We utilze the Semantic Kernal in dotnet c# as a reference.

## Data
We will use publicly available SEC filings to demonstrate how to build a multi-turn chatbot that can answer questions on complex purchase agreements.  The data is available on the SEC website and can be downloaded in pdf format.  With a ai bot trying to reason over multiple of these SEC filings, we have to make sure the resul is well-grounded and accurate.  

Take this [Deopomed filing](https://www.sec.gov/Archives/edgar/data/1005201/000110465906080867/a06-25523_1ex10d1.htm) as example.  If you are looking for deal amount or purchas price, you won't find it searching by those terms.  

![purchase price](/img/purchase_price.png)
The correct answer is up to $8.5 million.

Another example of challenge is the representing law firms.  You have to find them under the 'copy to' section.
![representing law firms](/img/buyer_seller_law_firm.png)
Seller law firm is Heller Ehrman LLP and Buyer law firm is Greenberg Traurig, LLP.  The attention to persons are the representatives of the law firms.

## Setup

### Azure AI Studio / Azure Open AI
You will need to deploy a LLM (I am using gpt4-o) and a embedding model (I am using text-embedding-3-large).  For the sake of simplicity, we will be using API for authentication. For production, please use managed identity where you can.

### Azure AI Search
Use the [Integrated vectorization](https://github.com/Azure/azure-search-vector-samples/blob/main/demo-python/code/integrated-vectorization/azure-search-integrated-vectorization-sample.ipynb) notebook to upload, index and vectorize the documents.  The notebook will also show you how to query the documents.

**Make sure to set AZURE_OPENAI_EMBEDDING_DIMENSIONS=3072 in the environment variables.** The AI Search SDK at the time of writing this does not support setting the dimensions.

This is the same process as if you did a _Import and Vectorize_ in Azure AI Search except that you get to choose your embedding model.  The portal only lets you pick text-embedding-ada-002.

### Azure Blog Storage
You will need an Azure Storage account to hold the SEC filings.
Do upload multiple files in order to see the multi-turn chatbot in action. 

## Another Approach
I do want to stress that this is _one_ approach of solving the problem.  You can do another approach where you pre-process the documents and extract the information you need or enrich the document with metadata for better AI Search result.  This is a more deterministic approach and you can use the RAG model to answer questions.  However, the downside is that you have to know what you are looking for.  The multi-turn chatbot approach is more flexible and can handle more complex questions.  The downside is that it is more complex to implement.  You can also use a combination of both approaches to get the best of both worlds.  The choice is yours.

## The Code

### Prompting
The prompting is done in 2 places.  The first is appsettings.json where you set the system prompts instructing the LLM what to do.

Semantic Kernel's native functions allow different custom code to be called.  The kernel with AutoInvokeKernelFunctions will call the right function based on the user AND system input.

I broke down system prompt into multiple variables for easier reading.
They are concatenated in the code.

```json
 "PROMPT_SYSTEM_MESSAGE": "You are legal document assistant that helps find answer in stock purchase agreement / Securites Purchase Agreement / Asset Purchase Agreement.  These legal files are complex in nature so use your knowledge in legal to answer the user's questions. ",
  "PROMPT_SYSTEM_MESSAGE_2":"Answer the questions as accurately as possible using the provided functions. Only use one function at a time. "
 ````

 Now we told kernel and LLM to call provided functions and only call one at a time, let's look at the native functions.