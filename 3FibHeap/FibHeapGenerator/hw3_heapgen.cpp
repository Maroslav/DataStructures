// Generates data for a Fibonacci heap

#include <string.h>
#include <stdio.h>
#include <stdlib.h>
#include <math.h>
#include "getopt.h"

#define MAX_LEN (2000000)
#define MIN_LEN (100000)
#define DIF_LEN (100000)
#define BIAS (1000)
#define NAIVE_MAX (27)
#define NAIVE_MIN (1)
#define NAIVE_DIF (1)

#define RAND() (((rand()) << 16) | (rand()))

int a[MAX_LEN + 1];

static void usage(void)
{
    fprintf(stderr, "Usage: heapgen [-s <student-id>] [-r] [-b] [-x]\n");
    exit(1);
}

/* Consolidate into one tree with high order and restore the heap structure after that
*/
static void expensive_loop(int loops) {
    //     fprintf(stderr,"expensive loop for loop=%d\n",loop);
    for (int i = 0; i < loops; i++) {
        printf("INS 1 1\n");
        printf("INS 2 2\n");
        printf("DEL\n");
        printf("DEL\n");
    }
}

/* Construct a star with n nodes and root index r
*/
static void star(int n, int r, bool consolidate, int c, int N) {
    //     fprintf(stderr,"star size %d from %d\n", n,r);
    if (n == 1) {
        //add a single node
        printf("INS %d %d\n", r, r);

        //consolidate everything if this is the second star of size 1 added
        if (consolidate) {
            //             fprintf(stderr,"consolidate\n");
            printf("INS 1 1\n");
            printf("DEL\n");
        }
    }
    else {
        //build two stars with n-1 vertices and consolidate them
        star(n - 1, r, false, 0, N);
        star(n - 1, r + n - 1, true, c + 1, N);

        //delete unnecessary parts
        for (int l = r + n; l < r + 2 * n - 2; l++) {
            //             fprintf(stderr,"delete node %d\n",l);
            printf("DEC %d %d\n", l, 1);
            printf("DEL\n");
        }
    }

}

/* generates a sequence on which non-cascading heaps need lots of time
* source: "Replacing Mark Bits with Randomness in Fibonacci Heaps" Jerry Li and John Peebles, MIT
* -> modification so that only a quadratic number of elements are needed in the star construction
*/
static void ncascade_gen_mod(void)
{
    int power, nr, from;

    for (int N = NAIVE_MIN; N <= NAIVE_MAX; N += NAIVE_DIF) { //main loop
        from = 3; //reserve two vertices with smaller keys than any other
        nr = from + N*(N + 1) / 2; //the number of needed elements

        printf("# %i\n", nr);
        //         fprintf(stderr,"test of size %i for N=%i\n",nr,N);

        //construct N stars of size n
        for (int n = N; n >= 1; n--) {
            star(n, from, false, 0, N);
            from += n;
        }

        //         fprintf(stderr,"starting expensive loop\n");
        //do some more expensive loops
        expensive_loop(10 * (1 << N));
    }//end main loop
}

/* generates a sequence with a bias towards fewer delete-mins
* E[nr of decrease keys] = 2 * E[nr of inserts]
* E[delete]= 0.6 * E[nr of inserts] / (bias+1)
* bias=0: totally random, according to above expected values
*/
static void random_gen(int bias)
{
    int next, op, j, nr_del, nr_dec;

    for (int length = MIN_LEN; length <= MAX_LEN; length += DIF_LEN) {
        for (int i = 0; i < length; i++) // initialize some random elements to insert
            a[i] = ((int)RAND()) % length;

        //         fprintf(stderr,"test of length %i\n",length);
        printf("# %d\n", length);
        next = 0; //next element to insert
        nr_del = floor(length / (bias + 1.6));
        nr_dec = floor(2 * length + 5.2*nr_del);

        while (next < 10) { //insert the first few elements to have something to work with
            printf("INS %d %d\n", next, a[next]);
            next++;
        }

        while (next < length) {
            op = ((int)RAND()) % (length + nr_dec + nr_del); //choose an operation at random

            if (op < length) { //insert the next element
                printf("INS %d %d\n", next, a[next]);
                next++;
            }
            else if (op < length + nr_dec) { //decrease the key of some element
                j = ((int)RAND()) % next;
                if (a[j] > 0) { //key of element can be decreased
                    a[j] -= (((int)RAND()) % next) + 1; //subtract some positive number
                    if (a[j] < 0) a[j] = 0;
                    printf("DEC %d %d\n", j, a[j]);
                }
            }
            else { //delete the minimum
                printf("DEL\n");
            }
        }
    }

}

int main(int argc, char* argv[])
{
    bool few_del_min = false, cascade = false, random = false;
    int opt, student_id = -1;

    if (argc > 1 && !strcmp(argv[1], "--help"))
        usage();

    while ((opt = getopt(argc, argv, "rbxs:")) >= 0)
        switch (opt) {
        case 's': student_id = atoi(optarg); break;
        case 'b': few_del_min = true; break;
        case 'x': cascade = true; break;
        case 'r': random = true; break;
        default: usage();
        }

    if (student_id < 0) {
        fprintf(stderr, "WARNING: Student ID not given, defaulting to 42.\n");
        student_id = 42;
    }

    if ((few_del_min ? 1 : 0) + (cascade ? 1 : 0) + (random ? 1 : 0) >= 2) {
        fprintf(stderr, "ERROR: Two or more tests given. Please, choose exactly one test.\n");
        usage();
    }


    srand(student_id);

    if (few_del_min == true) random_gen(BIAS);
    else if (cascade == true) ncascade_gen_mod();
    else if (random == true) random_gen(0);
    else fprintf(stderr, "ERROR: No test given. You have to choose -r for the random test or -b for the biased test or -x for the special test.\n");


    return 0;
}
