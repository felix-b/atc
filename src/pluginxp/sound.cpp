// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/COPYING
// 
#if 0

#include <stdio.h>
#include <string.h>
#include <stdlib.h>

#include "XPLMUtilities.h"
#include "XPLMProcessing.h"
#include "XPLMPlugin.h"

// OS X: we use this to convert our file path.
#if APL
#include <Carbon/Carbon.h>
#endif

// Your include paths for OpenAL may vary by platform.
#include <AL/al.h>
#include <AL/alc.h>

#include "utils.h"

/**************************************************************************************************************
 * WAVE FILE LOADING
**************************************************************************************************************/

// You can just use alutCreateBufferFromFile to load a wave file, but there seems to be a lot of problems with 
// alut not beign available, being deprecated, etc.  So...here's a stupid routine to load a wave file.  I have
// tested this only on x86 machines, so if you find a bug on PPC please let me know.

// Macros to swap endian-values.

#define SWAP_32(value)                 \
        (((((unsigned short)value)<<8) & 0xFF00)   | \
         ((((unsigned short)value)>>8) & 0x00FF))

#define SWAP_16(value)                     \
        (((((unsigned int)value)<<24) & 0xFF000000)  | \
         ((((unsigned int)value)<< 8) & 0x00FF0000)  | \
         ((((unsigned int)value)>> 8) & 0x0000FF00)  | \
         ((((unsigned int)value)>>24) & 0x000000FF))

// Wave files are RIFF files, which are "chunky" - each section has an ID and a length.  This lets us skip
// things we can't understand to find the parts we want.  This header is common to all RIFF chunks.
struct chunk_header { 
	int			id;
	int			size;
};

// WAVE file format info.  We pass this through to OpenAL so we can support mono/stereo, 8/16/bit, etc.
struct format_info {
	short		format;				// PCM = 1, not sure what other values are legal.
	short		num_channels;
	int			sample_rate;
	int			byte_rate;
	short		block_align;
	short		bits_per_sample;
};

// This utility returns the start of data for a chunk given a range of bytes it might be within.  Pass 1 for
// swapped if the machine is not the same endian as the file.
static char *	find_chunk(char * file_begin, char * file_end, int desired_id, int swapped)
{
	while(file_begin < file_end)
	{
		chunk_header * h = (chunk_header *) file_begin;
		if(h->id == desired_id && !swapped)
			return file_begin+sizeof(chunk_header);
		if(h->id == SWAP_32(desired_id) && swapped)
			return file_begin+sizeof(chunk_header);
		int chunk_size = swapped ? SWAP_32(h->size) : h->size;
		char * next = file_begin + chunk_size + sizeof(chunk_header);
		if(next > file_end || next <= file_begin)
			return NULL;
		file_begin = next;		
	}
	return NULL;
}

// Given a chunk, find its end by going back to the header.
static char * chunk_end(char * chunk_start, int swapped)
{
	chunk_header * h = (chunk_header *) (chunk_start - sizeof(chunk_header));
	return chunk_start + (swapped ? SWAP_32(h->size) : h->size);
}

#define FAIL(X) { XPLMDebugString(X); free(mem); return 0; }

#define RIFF_ID 0x46464952			// 'RIFF'
#define FMT_ID  0x20746D66			// 'FMT '
#define DATA_ID 0x61746164			// 'DATA'

ALuint load_wave(const char * file_name)
{
	// First: we open the file and copy it into a single large memory buffer for processing.

	FILE * fi = fopen(file_name,"rb");
	if(fi == NULL)
	{
		XPLMDebugString("TNCAL> WAVE file load failed - could not open.\n");	
		return 0;
	}
	fseek(fi,0,SEEK_END);
	int file_size = ftell(fi);
	fseek(fi,0,SEEK_SET);
	char * mem = (char*) malloc(file_size);
	if(mem == NULL)
	{
		XPLMDebugString("TNCAL> WAVE file load failed - could not allocate memory.\n");
		fclose(fi);
		return 0;
	}
	if (fread(mem, 1, file_size, fi) != file_size)
	{
		XPLMDebugString("TNCAL> WAVE file load failed - could not read file.\n");	
		free(mem);
		fclose(fi);
		return 0;
	}
	fclose(fi);
	char * mem_end = mem + file_size;
	
	// Second: find the RIFF chunk.  Note that by searching for RIFF both normal
	// and reversed, we can automatically determine the endian swap situation for
	// this file regardless of what machine we are on.
	
	int swapped = 0;
	char * riff = find_chunk(mem, mem_end, RIFF_ID, 0);
	if(riff == NULL)
	{
		riff = find_chunk(mem, mem_end, RIFF_ID, 1);
		if(riff)
			swapped = 1;
		else
			FAIL("TNCAL> Could not find RIFF chunk in wave file.\n")
	}
	
	// The wave chunk isn't really a chunk at all. :-(  It's just a "WAVE" tag 
	// followed by more chunks.  This strikes me as totally inconsistent, but
	// anyway, confirm the WAVE ID and move on.
	
	if (riff[0] != 'W' ||
		riff[1] != 'A' ||
		riff[2] != 'V' ||
		riff[3] != 'E')
		FAIL("TNCAL> Could not find WAVE signature in wave file.\n")

	char * format = find_chunk(riff+4, chunk_end(riff,swapped), FMT_ID, swapped);
	if(format == NULL)
		FAIL("TNCAL> Could not find FMT  chunk in wave file.\n")
	
	// Find the format chunk, and swap the values if needed.  This gives us our real format.
	
	format_info * fmt = (format_info *) format;
	if(swapped)
	{
		fmt->format = SWAP_16(fmt->format);
		fmt->num_channels = SWAP_16(fmt->num_channels);
		fmt->sample_rate = SWAP_32(fmt->sample_rate);
		fmt->byte_rate = SWAP_32(fmt->byte_rate);
		fmt->block_align = SWAP_16(fmt->block_align);
		fmt->bits_per_sample = SWAP_16(fmt->bits_per_sample);
	}
	
	// Reject things we don't understand...expand this code to support weirder audio formats.
	
	if(fmt->format != 1) FAIL("TNCAL> Wave file is not PCM format data.\n")
	if(fmt->num_channels != 1 && fmt->num_channels != 2) FAIL("TNCAL> Must have mono or stereo sound.\n")
	if(fmt->bits_per_sample != 8 && fmt->bits_per_sample != 16) FAIL("TNCAL> Must have 8 or 16 bit sounds.\n")
	char * data = find_chunk(riff+4, chunk_end(riff,swapped), DATA_ID, swapped) ;
	if(data == NULL)
		FAIL("TNCAL> I could not find the DATA chunk.\n")
	
	int sample_size = fmt->num_channels * fmt->bits_per_sample / 8;
	int data_bytes = chunk_end(data,swapped) - data;
	int data_samples = data_bytes / sample_size;
	
	// If the file is swapped and we have 16-bit audio, we need to endian-swap the audio too or we'll 
	// get something that sounds just astoundingly bad!
	
	if(fmt->bits_per_sample == 16 && swapped)
	{
		short * ptr = (short *) data;
		int words = data_samples * fmt->num_channels;
		while(words--)
		{
			*ptr = SWAP_16(*ptr);
			++ptr;
		}
	}
	
	// Finally, the OpenAL crud.  Build a new OpenAL buffer and send the data to OpenAL, passing in
	// OpenAL format enums based on the format chunk.
	
	ALuint buf_id = 0;
	alGenBuffers(1, &buf_id);
	if(buf_id == 0) FAIL("TNCAL> Could not generate buffer id.\n");
	
	alBufferData(buf_id, fmt->bits_per_sample == 16 ? 
							(fmt->num_channels == 2 ? AL_FORMAT_STEREO16 : AL_FORMAT_MONO16) :
							(fmt->num_channels == 2 ? AL_FORMAT_STEREO8 : AL_FORMAT_MONO8),
					data, data_bytes, fmt->sample_rate);
	free(mem);
	return buf_id;
}




/**************************************************************************************************************
 * SAMPLE OEPNAL PLUGIN:
 **************************************************************************************************************/
 
static ALuint			snd_src		=0;				// Sample source and buffer - this is one "sound" we play.

static ALCdevice *		my_dev		= NULL;			// We make our own device and context to play sound through.
static ALCcontext *		my_ctx		= NULL;


// This is a stupid logging error function...useful for debugging, but not good error checking.
#define CHECK_ERR() __CHECK_ERR(__FILE__,__LINE__)
static void __CHECK_ERR(const char * f, int l)
{
	ALuint e = alGetError();
	if (e != AL_NO_ERROR)
		PrintDebugString("ERROR: %d (%s:%d\n", e, f, l);
}

// Mac specific: this converts file paths from HFS (which we get from the SDK) to Unix (which the OS wants).
// See this for more info:
//
// http://www.xsquawkbox.net/xpsdk/mediawiki/FilePathsAndMacho

#if APL
static int ConvertPath(const char * inPath, char * outPath, int outPathMaxLen) {

	CFStringRef inStr = CFStringCreateWithCString(kCFAllocatorDefault, inPath ,kCFStringEncodingMacRoman);
	if (inStr == NULL)
		return -1;
	CFURLRef url = CFURLCreateWithFileSystemPath(kCFAllocatorDefault, inStr, kCFURLHFSPathStyle,0);
	CFStringRef outStr = CFURLCopyFileSystemPath(url, kCFURLPOSIXPathStyle);
	if (!CFStringGetCString(outStr, outPath, outPathMaxLen, kCFURLPOSIXPathStyle))
		return -1;
	CFRelease(outStr);
	CFRelease(url);
	CFRelease(inStr);
	return 0;
}
#endif

// Initialization code.

bool initializeOpenALContext()
{
	CHECK_ERR();

	// We have to save the old context and restore it later, so that we don't interfere with X-Plane
	// and other plugins.

	ALCcontext * old_ctx = alcGetCurrentContext();
	
	if(old_ctx == NULL)
	{
		PrintDebugString("TNCAL> 0x%08x: I found no OpenAL, I will be the first to init.\n",XPLMGetMyID());
		my_dev = alcOpenDevice(NULL);
		if(my_dev == NULL)
		{
			XPLMDebugString("TNCAL> Could not open the default OpenAL device.\n");
			return false;		
		}	
		my_ctx = alcCreateContext(my_dev, NULL);
		if(my_ctx == NULL)
		{
			if(old_ctx)
				alcMakeContextCurrent(old_ctx);
			alcCloseDevice(my_dev);
			my_dev = NULL;
			XPLMDebugString("TNCAL> Could not create a context.\n");
			return false;				
		}
		
		// Make our context current, so that OpenAL commands affect our, um, stuff.
		
		alcMakeContextCurrent(my_ctx);
		PrintDebugString("TNCAL> 0x%08x: I created the context.\n",XPLMGetMyID(), my_ctx);

		ALCint		major_version, minor_version;
		const char * al_hw=alcGetString(my_dev,ALC_DEVICE_SPECIFIER	);
		const char * al_ex=alcGetString(my_dev,ALC_EXTENSIONS);
		alcGetIntegerv(NULL,ALC_MAJOR_VERSION,sizeof(major_version),&major_version);
		alcGetIntegerv(NULL,ALC_MINOR_VERSION,sizeof(minor_version),&minor_version);
		
		PrintDebugString("TNCAL> OpenAL version   : %d.%d\n",major_version,minor_version);
		PrintDebugString("TNCAL> OpenAL hardware  : %s\n", (al_hw?al_hw:"(none)"));
		PrintDebugString("TNCAL> OpenAL extensions: %s\n", (al_ex?al_ex:"(none)"));
		CHECK_ERR();

	} 
	else
	{
		PrintDebugString("TNCAL> 0x%08x: I found someone else's context 0x%08x.\n",XPLMGetMyID(), old_ctx);
	}
	
	ALfloat	zero[3] = { 0 } ;

	// Generate 1 source and load a buffer of audio.
	alGenSources(1,&snd_src);
	CHECK_ERR();
	
	// Basic initializtion code to play a sound: specify the buffer the source is playing, as well as some 
	// sound parameters. This doesn't play the sound - it's just one-time initialization.
	alSourcef(snd_src,AL_PITCH,1.0f);
	alSourcef(snd_src,AL_GAIN,1.0f);	
	alSourcei(snd_src,AL_LOOPING,0);
	alSourcefv(snd_src,AL_POSITION, zero);
	alSourcefv(snd_src,AL_VELOCITY, zero);
	CHECK_ERR();

	return true;
}

void finalizeOpenALConext()
{
	// Cleanup: nuke our context if we have it.  This is hacky and bad - we should really destroy
	// our buffers and sources.  I have _no_ idea if OpenAL will leak memory.
	if(alcGetCurrentContext() != NULL)
	{
		PrintDebugString("TNCAL> 0x%08x: deleting snd src %d\n", XPLMGetMyID());
		if(snd_src)		alDeleteSources(1,&snd_src);
	}
	if(my_ctx) 
	{
		PrintDebugString("TNCAL> 0x%08x: deleting my context 0x%08x\n", XPLMGetMyID(),my_ctx);
		alcMakeContextCurrent(NULL);
		alcDestroyContext(my_ctx);
	}
	if(my_dev) alcCloseDevice(my_dev);
}

bool playSoundFromWaveFile(const std::string& waveFilePath)
{
    ALuint snd_buffer = load_wave(waveFilePath.c_str());
    if (!snd_buffer)
    {
        return false;
    }
	
    PrintDebugString("TNCAL> 0x%08x: Loaded %d from %s\n", XPLMGetMyID(), snd_buffer, waveFilePath.c_str());
	CHECK_ERR();

	alSourcei(snd_src,AL_BUFFER,snd_buffer);
    alSourcef(snd_src,AL_PITCH,1.0f);
    alSourcePlay(snd_src);
    CHECK_ERR();			

    PrintDebugString("0x%08x: deleting snd %d\n", XPLMGetMyID(),snd_buffer);
    if(snd_buffer) alDeleteBuffers(1,&snd_buffer);

    return true;
}

#endif