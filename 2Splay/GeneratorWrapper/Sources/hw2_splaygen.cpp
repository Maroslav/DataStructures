// Generates data

#include <string.h>
#include <fcntl.h>
#include <stdio.h>
#include <time.h>
#include <stdlib.h>
#include <io.h>
#include <iostream>
#include <unistd.h>
#include <stdbool.h>
#include <getopt.h>
#include <assert.h>

using namespace System;


static int generate(int argc, char* argv[]);

namespace GeneratorWrapper
{
    public ref class OpGeneratorWrapper
    {
    public:
        void Generate(array<String^>^ args)
        {
            char** tokensAsUtf8 = new char*[args->Length];

            for (int i = 0; i < args->Length; i++)
            {
                array<Byte>^ encodedBytes = Text::Encoding::UTF8->GetBytes(args[i]);

                // Probably just using [0] is fine here
                pin_ptr<Byte> pinnedBytes = &encodedBytes[encodedBytes->GetLowerBound(0)];

                tokensAsUtf8[i] = new char[encodedBytes->Length + 1];
                memcpy(
                    tokensAsUtf8[i],
                    reinterpret_cast<char*>(pinnedBytes),
                    encodedBytes->Length
                );

                // NULL-terminate the native string
                tokensAsUtf8[i][encodedBytes->Length] = '\0';
            }

            generate(args->Length, tokensAsUtf8);


            for (int i = 0; i < args->Length; i++)
                delete[] tokensAsUtf8[i];

            delete[] tokensAsUtf8;
        }
    };
}


#define MAX_LEN  (10000000)

int a[MAX_LEN + 1];

static void usage(void)
{
    fprintf(stderr, "Usage: gen [-s <student-id>] [-t <size-of-subset>] [-b] [-l]\n");
    exit(1);
}

static int nextrandom()
{
    return ((int)rand()) * ((int)rand()) + (int)rand();
}

static int nextrandom2(int A, int B)
{
    if (B <= A)
        return A;
    int offset = (nextrandom() % (1 + B - A));
    if (offset < 0)
        return A - offset; else return A + offset;
}

static void swapa(int i, int j)
{
    int t = a[i];
    a[i] = a[j];
    a[j] = t;
}

static void randomize(int len)
{
    for (int i = 0; i < len; i++)
        swapa(i, nextrandom2(0, len - 1));
}

static void makeprogression(int A, int B, int s, int inc, int len)
{
    for (int i = 0; i < len; i++)
        if (i == s + inc*(a[i] - A));
        else if ((A <= a[i]) && (a[i] <= B))
        {
            swapa(i, s + inc*(a[i] - A));
            i--;
        }
}

static void sequential_generator(void)
{
    for (int elements = 100; elements <= 2000; elements += 100)
    {
        printf("# %d\n", elements);

        for (int i = 1; i <= elements; ++i)
            printf("I %d\n", i);

        for (int j = 0; j < 2; ++j)
            for (int i = 1; i <= elements / 2; ++i)
                printf("F %d\n", i);
    }
}

static int generate(int argc, char* argv[])
{
    bool sequential = false, last = false;
    int opt, student_id = -1, subset_size = -1;

    if (argc > 1 && !strcmp(argv[1], "--help"))
        usage();

    while ((opt = getopt(argc, argv, "bls:t:")) >= 0)
        switch (opt)
        {
        case 's': student_id = atoi(optarg); break;
        case 't': subset_size = atoi(optarg); break;
        case 'b': sequential = true; break;
        case 'l': last = true; break;
        default: usage();
        }

    if ((sequential == true) ^ (subset_size < 0))
    {
        fprintf(stderr, "Invalid generator: Use either '-t <size-of-subset>' for random test or '-b' for sequential test.\n");
        return 1;
    }

    if (sequential)
    {
        sequential_generator();
        return 0;
    }

    if (student_id < 0)
    {
        fprintf(stderr, "WARNING: Student ID not given, defaulting to 42.\n");
        student_id = 42;
    }

    if (subset_size < 10 || subset_size > 1000000)
    {
        fprintf(stderr, "The size of searched subset must be between 10 and 1000000.\n");
        return 1;
    }

    srand(student_id);

    for (int length = 1000; length <= 1000000; length += 3000)
    {
        for (int i = 0; i < length; i++)
            a[i] = i;

        randomize(length);
        makeprogression(length / 4, length / 4 + length / 20, length / 10, 1, length);
        makeprogression(length / 2, length / 2 + length / 20, length / 10, -1, length);
        makeprogression(3 * length / 4, 3 * length / 4 + length / 20, length / 2, -4, length);
        makeprogression(17 * length / 20, 17 * length / 20 + length / 20, 2 * length / 5, 5, length);

        printf("# %d\n", length);
        for (int i = length - 1; i >= 0; i--)
            printf("I %d\n", a[i]);

        if (!last)
            randomize(length);

        int r = subset_size < length ? subset_size : length;

        int iters = 100;
        if (r <= 100)
            iters = 1000;
        else if (r >= 100000)
            iters = 20;
        else if (r >= 200000)
            iters = 5;
        else if (r >= 500000)
            iters = 2;

        for (int j = 0; j < iters*r; j++)
            printf("F %d\n", a[nextrandom2(0, r - 1)]);
    }

    return 0;
}
