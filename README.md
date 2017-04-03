# alexa-audio-tutorial

This project is a bare bones Alexa audio skill written in C# and intended for deployment on Amazon's Lambda service. I walk through [setup, core code concepts, and deployment in this tutorial](http://matthiasshapiro.com/2017/04/01/alexa-audio-skill-in-c-net-core/).

Herein, you will find the following components:

## Alexa Audio Function

This function handles 

- persistent user state using a DynamoDb Helper
- Launch requests
- Intent requests
- Audio-specific requests

## Audio Assets

In the speechAssets folder, I've included five 30-second audio files for testing. Each file starts with "This is the [first, second, third, etc] of five audio files" and then plays a sample of Bach (so it's not too annoying to play over and over).

I've found this helpful because you don't have to wait 10+ minutes to test events like PlaybackFinished or PlaybackNearlyFinished and having the audio files audibly declare their location in a sequence lets us test shuffle and next / previous functionality without having to look at the logs.

## Sample Json Requests / Responses

Also in the speechAssets folder, there are several sample request / response json objects that can be used for unit testing or other non-deployment testing for Alexa skills. 

## DynamoDB helper

This is a very simplistic helper and not particularly extensible, but it is easy to read. It has methods for verifying / creating a table for our user state as well as saving & retrieving that state.

