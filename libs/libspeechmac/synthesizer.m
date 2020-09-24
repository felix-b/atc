// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/COPYING
// 

/*
 * not working build command: gcc -framework Foundation hello.m
 * working build command: clang -framework Foundation -framework Cocoa hello.m -o hello
 * working build command: g++ -c -ObjC hello.m -m64
 */

#import <Foundation/Foundation.h>
#import <Cocoa/Cocoa.h>
#import "synthesizer.h"

int getGenderCode(NSString *genderString);

@interface FinishDetectorDelegate<NSSpeechSynthesizerDelegate> : NSObject
- (void)speechSynthesizer:(NSSpeechSynthesizer *)sender didFinishSpeaking:(BOOL) finishedSpeaking;
@end

@implementation FinishDetectorDelegate
static NSInteger _finishedCount = 0;
static CFRunLoopRef _runLoopRef;
- (void)speechSynthesizer:(NSSpeechSynthesizer *)sender didFinishSpeaking:(BOOL) finishedSpeaking {
    CFRunLoopStop(_runLoopRef);
  }
- (void)setCurrentRunLoopRef
  {
    _runLoopRef = CFRunLoopGetCurrent();
  }
@end

int cocoaSynthesizeSpeech(const char *voiceId, const char *text, const char *filePath)
{
  NSString *voiceIdString = [NSString stringWithUTF8String:voiceId];
  NSSpeechSynthesizer *synthesizer = [[NSSpeechSynthesizer alloc] initWithVoice:voiceIdString];
  FinishDetectorDelegate *finishDetector = [[FinishDetectorDelegate alloc] init];

  [finishDetector setCurrentRunLoopRef];
  synthesizer.delegate = finishDetector;

  NSString *textString = [NSString stringWithUTF8String:text];
  NSString *filePathString = [NSString stringWithUTF8String:filePath];
  NSURL *url = [NSURL fileURLWithPath:filePathString];

  BOOL result = [synthesizer startSpeakingString:textString toURL:url];
  if (!result) {
    return 0;
  }
  CFRunLoopRun(); // stopped by finish detector
  return 1;
}

int cocoaEnumSpeechVoices(CocoaEnumSpeechVoicesCallback callback)
{
  for (NSString *voiceId in [NSSpeechSynthesizer availableVoices]) {
      NSString *localeId = [[NSSpeechSynthesizer attributesForVoice:voiceId] objectForKey:NSVoiceLocaleIdentifier];
      
      if ([localeId isEqualToString:@"en"] || [localeId hasPrefix:@"en_"]) {
        NSString *genderString = [[NSSpeechSynthesizer attributesForVoice:voiceId] objectForKey:NSVoiceGender];
        int genderCode = getGenderCode(genderString);
        (*callback)([voiceId UTF8String], genderCode);
      }
  }
  return 1;
}

int getGenderCode(NSString *genderString)
{
  if ([genderString isEqualToString:@"VoiceGenderMale"]) {
    return GENDER_MALE;
  }
  if ([genderString isEqualToString:@"VoiceGenderFemale"]) {
    return GENDER_FEMALE;
  }
  return 0;
}
