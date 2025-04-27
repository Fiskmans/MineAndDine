#[compute]
#version 450

// Invocations in the (x, y, z) dimension
layout(local_size_x = 1, local_size_y = 1, local_size_z = 1) in;

layout(set = 0, binding = 0, std430) restrict buffer InputStatic
{
    uvec3 Size;
    uint MaxTris;
};

layout(set = 0, binding = 1, std430) restrict buffer InputUniform
{
    uint Surface;
    float Scale;
};

layout(set = 0, binding = 2, std430) restrict buffer Input 
{
    int NodeData[];
};


struct Tri
{
    vec4 a;
    vec4 b;
    vec4 c;
};

layout(set = 0, binding = 3, std430) restrict buffer OutputCount
{
    uint TriCount;
};

layout(set = 0, binding = 4, std430) restrict buffer Output
{
    Tri Tris[];
};

// blame the source for weird order
// Adaptation of https://paulbourke.net/geometry/polygonise/
const uvec3[8] Corners = uvec3[8]
(
    uvec3(0, 0, 0),
    uvec3(1, 0, 0),
    uvec3(1, 1, 0),
    uvec3(0, 1, 0),
    uvec3(0, 0, 1),
    uvec3(1, 0, 1),
    uvec3(1, 1, 1),
    uvec3(0, 1, 1)
);

const uint MaxTrisPer = 5;

const ivec3 NoTri = ivec3(-1, -1, -1);

const ivec3[128 * MaxTrisPer] TriTable = ivec3[128 * MaxTrisPer] (
    NoTri,             NoTri,             NoTri,             NoTri,             NoTri,            
    ivec3( 0,  8,  3), NoTri,             NoTri,             NoTri,             NoTri,            
    ivec3( 0,  1,  9), NoTri,             NoTri,             NoTri,             NoTri,            
    ivec3( 1,  8,  3), ivec3( 9,  8,  1), NoTri,             NoTri,             NoTri,            
    ivec3( 1,  2, 10), NoTri,             NoTri,             NoTri,             NoTri,            
    ivec3( 0,  8,  3), ivec3( 1,  2, 10), NoTri,             NoTri,             NoTri,            
    ivec3( 9,  2, 10), ivec3( 0,  2,  9), NoTri,             NoTri,             NoTri,            
    ivec3( 2,  8,  3), ivec3( 2, 10,  8), ivec3(10,  9,  8), NoTri,             NoTri,            
    ivec3( 3, 11,  2), NoTri,             NoTri,             NoTri,             NoTri,            
    ivec3( 0, 11,  2), ivec3( 8, 11,  0), NoTri,             NoTri,             NoTri,            
    ivec3( 1,  9,  0), ivec3( 2,  3, 11), NoTri,             NoTri,             NoTri,            
    ivec3( 1, 11,  2), ivec3( 1,  9, 11), ivec3( 9,  8, 11), NoTri,             NoTri,            
    ivec3( 3, 10,  1), ivec3(11, 10,  3), NoTri,             NoTri,             NoTri,            
    ivec3( 0, 10,  1), ivec3( 0,  8, 10), ivec3( 8, 11, 10), NoTri,             NoTri,            
    ivec3( 3,  9,  0), ivec3( 3, 11,  9), ivec3(11, 10,  9), NoTri,             NoTri,            
    ivec3( 9,  8, 10), ivec3(10,  8, 11), NoTri,             NoTri,             NoTri,              // 0x10
    ivec3( 4,  7,  8), NoTri,             NoTri,             NoTri,             NoTri,            
    ivec3( 4,  3,  0), ivec3( 7,  3,  4), NoTri,             NoTri,             NoTri,            
    ivec3( 0,  1,  9), ivec3( 8,  4,  7), NoTri,             NoTri,             NoTri,            
    ivec3( 4,  1,  9), ivec3( 4,  7,  1), ivec3( 7,  3,  1), NoTri,             NoTri,            
    ivec3( 1,  2, 10), ivec3( 8,  4,  7), NoTri,             NoTri,             NoTri,            
    ivec3( 3,  4,  7), ivec3( 3,  0,  4), ivec3( 1,  2, 10), NoTri,             NoTri,            
    ivec3( 9,  2, 10), ivec3( 9,  0,  2), ivec3( 8,  4,  7), NoTri,             NoTri,            
    ivec3( 2, 10,  9), ivec3( 2,  9,  7), ivec3( 2,  7,  3), ivec3( 7,  9,  4), NoTri,            
    ivec3( 8,  4,  7), ivec3( 3, 11,  2), NoTri,             NoTri,             NoTri,            
    ivec3(11,  4,  7), ivec3(11,  2,  4), ivec3( 2,  0,  4), NoTri,             NoTri,            
    ivec3( 9,  0,  1), ivec3( 8,  4,  7), ivec3( 2,  3, 11), NoTri,             NoTri,            
    ivec3( 4,  7, 11), ivec3( 9,  4, 11), ivec3( 9, 11,  2), ivec3( 9,  2,  1), NoTri,            
    ivec3( 3, 10,  1), ivec3( 3, 11, 10), ivec3( 7,  8,  4), NoTri,             NoTri,            
    ivec3( 1, 11, 10), ivec3( 1,  4, 11), ivec3( 1,  0,  4), ivec3( 7, 11,  4), NoTri,            
    ivec3( 4,  7,  8), ivec3( 9,  0, 11), ivec3( 9, 11, 10), ivec3(11,  0,  3), NoTri,            
    ivec3( 4,  7, 11), ivec3( 4, 11,  9), ivec3( 9, 11, 10), NoTri,             NoTri,              // 0x20
    ivec3( 9,  5,  4), NoTri,             NoTri,             NoTri,             NoTri,            
    ivec3( 9,  5,  4), ivec3( 0,  8,  3), NoTri,             NoTri,             NoTri,            
    ivec3( 0,  5,  4), ivec3( 1,  5,  0), NoTri,             NoTri,             NoTri,            
    ivec3( 8,  5,  4), ivec3( 8,  3,  5), ivec3( 3,  1,  5), NoTri,             NoTri,            
    ivec3( 1,  2, 10), ivec3( 9,  5,  4), NoTri,             NoTri,             NoTri,            
    ivec3( 3,  0,  8), ivec3( 1,  2, 10), ivec3( 4,  9,  5), NoTri,             NoTri,            
    ivec3( 5,  2, 10), ivec3( 5,  4,  2), ivec3( 4,  0,  2), NoTri,             NoTri,            
    ivec3( 2, 10,  5), ivec3( 3,  2,  5), ivec3( 3,  5,  4), ivec3( 3,  4,  8), NoTri,            
    ivec3( 9,  5,  4), ivec3( 2,  3, 11), NoTri,             NoTri,             NoTri,            
    ivec3( 0, 11,  2), ivec3( 0,  8, 11), ivec3( 4,  9,  5), NoTri,             NoTri,            
    ivec3( 0,  5,  4), ivec3( 0,  1,  5), ivec3( 2,  3, 11), NoTri,             NoTri,            
    ivec3( 2,  1,  5), ivec3( 2,  5,  8), ivec3( 2,  8, 11), ivec3( 4,  8,  5), NoTri,            
    ivec3(10,  3, 11), ivec3(10,  1,  3), ivec3( 9,  5,  4), NoTri,             NoTri,            
    ivec3( 4,  9,  5), ivec3( 0,  8,  1), ivec3( 8, 10,  1), ivec3( 8, 11, 10), NoTri,            
    ivec3( 5,  4,  0), ivec3( 5,  0, 11), ivec3( 5, 11, 10), ivec3(11,  0,  3), NoTri,            
    ivec3( 5,  4,  8), ivec3( 5,  8, 10), ivec3(10,  8, 11), NoTri,             NoTri,              // 0x30
    ivec3( 9,  7,  8), ivec3( 5,  7,  9), NoTri,             NoTri,             NoTri,            
    ivec3( 9,  3,  0), ivec3( 9,  5,  3), ivec3( 5,  7,  3), NoTri,             NoTri,            
    ivec3( 0,  7,  8), ivec3( 0,  1,  7), ivec3( 1,  5,  7), NoTri,             NoTri,            
    ivec3( 1,  5,  3), ivec3( 3,  5,  7), NoTri,             NoTri,             NoTri,            
    ivec3( 9,  7,  8), ivec3( 9,  5,  7), ivec3(10,  1,  2), NoTri,             NoTri,            
    ivec3(10,  1,  2), ivec3( 9,  5,  0), ivec3( 5,  3,  0), ivec3( 5,  7,  3), NoTri,            
    ivec3( 8,  0,  2), ivec3( 8,  2,  5), ivec3( 8,  5,  7), ivec3(10,  5,  2), NoTri,            
    ivec3( 2, 10,  5), ivec3( 2,  5,  3), ivec3( 3,  5,  7), NoTri,             NoTri,            
    ivec3( 7,  9,  5), ivec3( 7,  8,  9), ivec3( 3, 11,  2), NoTri,             NoTri,            
    ivec3( 9,  5,  7), ivec3( 9,  7,  2), ivec3( 9,  2,  0), ivec3( 2,  7, 11), NoTri,            
    ivec3( 2,  3, 11), ivec3( 0,  1,  8), ivec3( 1,  7,  8), ivec3( 1,  5,  7), NoTri,            
    ivec3(11,  2,  1), ivec3(11,  1,  7), ivec3( 7,  1,  5), NoTri,             NoTri,            
    ivec3( 9,  5,  8), ivec3( 8,  5,  7), ivec3(10,  1,  3), ivec3(10,  3, 11), NoTri,            
    ivec3( 5,  7,  0), ivec3( 5,  0,  9), ivec3( 7, 11,  0), ivec3( 1,  0, 10), ivec3(11, 10,  0),
    ivec3(11, 10,  0), ivec3(11,  0,  3), ivec3(10,  5,  0), ivec3( 8,  0,  7), ivec3( 5,  7,  0),
    ivec3(11, 10,  5), ivec3( 7, 11,  5), NoTri,             NoTri,             NoTri,              // 0x40
    ivec3(10,  6,  5), NoTri,             NoTri,             NoTri,             NoTri,            
    ivec3( 0,  8,  3), ivec3( 5, 10,  6), NoTri,             NoTri,             NoTri,            
    ivec3( 9,  0,  1), ivec3( 5, 10,  6), NoTri,             NoTri,             NoTri,            
    ivec3( 1,  8,  3), ivec3( 1,  9,  8), ivec3( 5, 10,  6), NoTri,             NoTri,            
    ivec3( 1,  6,  5), ivec3( 2,  6,  1), NoTri,             NoTri,             NoTri,            
    ivec3( 1,  6,  5), ivec3( 1,  2,  6), ivec3( 3,  0,  8), NoTri,             NoTri,            
    ivec3( 9,  6,  5), ivec3( 9,  0,  6), ivec3( 0,  2,  6), NoTri,             NoTri,            
    ivec3( 5,  9,  8), ivec3( 5,  8,  2), ivec3( 5,  2,  6), ivec3( 3,  2,  8), NoTri,            
    ivec3( 2,  3, 11), ivec3(10,  6,  5), NoTri,             NoTri,             NoTri,            
    ivec3(11,  0,  8), ivec3(11,  2,  0), ivec3(10,  6,  5), NoTri,             NoTri,            
    ivec3( 0,  1,  9), ivec3( 2,  3, 11), ivec3( 5, 10,  6), NoTri,             NoTri,            
    ivec3( 5, 10,  6), ivec3( 1,  9,  2), ivec3( 9, 11,  2), ivec3( 9,  8, 11), NoTri,            
    ivec3( 6,  3, 11), ivec3( 6,  5,  3), ivec3( 5,  1,  3), NoTri,             NoTri,            
    ivec3( 0,  8, 11), ivec3( 0, 11,  5), ivec3( 0,  5,  1), ivec3( 5, 11,  6), NoTri,            
    ivec3( 3, 11,  6), ivec3( 0,  3,  6), ivec3( 0,  6,  5), ivec3( 0,  5,  9), NoTri,            
    ivec3( 6,  5,  9), ivec3( 6,  9, 11), ivec3(11,  9,  8), NoTri,             NoTri,              // 0x50
    ivec3( 5, 10,  6), ivec3( 4,  7,  8), NoTri,             NoTri,             NoTri,            
    ivec3( 4,  3,  0), ivec3( 4,  7,  3), ivec3( 6,  5, 10), NoTri,             NoTri,            
    ivec3( 1,  9,  0), ivec3( 5, 10,  6), ivec3( 8,  4,  7), NoTri,             NoTri,            
    ivec3(10,  6,  5), ivec3( 1,  9,  7), ivec3( 1,  7,  3), ivec3( 7,  9,  4), NoTri,            
    ivec3( 6,  1,  2), ivec3( 6,  5,  1), ivec3( 4,  7,  8), NoTri,             NoTri,            
    ivec3( 1,  2,  5), ivec3( 5,  2,  6), ivec3( 3,  0,  4), ivec3( 3,  4,  7), NoTri,            
    ivec3( 8,  4,  7), ivec3( 9,  0,  5), ivec3( 0,  6,  5), ivec3( 0,  2,  6), NoTri,            
    ivec3( 7,  3,  9), ivec3( 7,  9,  4), ivec3( 3,  2,  9), ivec3( 5,  9,  6), ivec3( 2,  6,  9),
    ivec3( 3, 11,  2), ivec3( 7,  8,  4), ivec3(10,  6,  5), NoTri,             NoTri,            
    ivec3( 5, 10,  6), ivec3( 4,  7,  2), ivec3( 4,  2,  0), ivec3( 2,  7, 11), NoTri,            
    ivec3( 0,  1,  9), ivec3( 4,  7,  8), ivec3( 2,  3, 11), ivec3( 5, 10,  6), NoTri,            
    ivec3( 9,  2,  1), ivec3( 9, 11,  2), ivec3( 9,  4, 11), ivec3( 7, 11,  4), ivec3( 5, 10,  6),
    ivec3( 8,  4,  7), ivec3( 3, 11,  5), ivec3( 3,  5,  1), ivec3( 5, 11,  6), NoTri,            
    ivec3( 5,  1, 11), ivec3( 5, 11,  6), ivec3( 1,  0, 11), ivec3( 7, 11,  4), ivec3( 0,  4, 11),
    ivec3( 0,  5,  9), ivec3( 0,  6,  5), ivec3( 0,  3,  6), ivec3(11,  6,  3), ivec3( 8,  4,  7),
    ivec3( 6,  5,  9), ivec3( 6,  9, 11), ivec3( 4,  7,  9), ivec3( 7, 11,  9), NoTri,              // 0x60
    ivec3(10,  4,  9), ivec3( 6,  4, 10), NoTri,             NoTri,             NoTri,            
    ivec3( 4, 10,  6), ivec3( 4,  9, 10), ivec3( 0,  8,  3), NoTri,             NoTri,            
    ivec3(10,  0,  1), ivec3(10,  6,  0), ivec3( 6,  4,  0), NoTri,             NoTri,            
    ivec3( 8,  3,  1), ivec3( 8,  1,  6), ivec3( 8,  6,  4), ivec3( 6,  1, 10), NoTri,            
    ivec3( 1,  4,  9), ivec3( 1,  2,  4), ivec3( 2,  6,  4), NoTri,             NoTri,            
    ivec3( 3,  0,  8), ivec3( 1,  2,  9), ivec3( 2,  4,  9), ivec3( 2,  6,  4), NoTri,            
    ivec3( 0,  2,  4), ivec3( 4,  2,  6), NoTri,             NoTri,             NoTri,            
    ivec3( 8,  3,  2), ivec3( 8,  2,  4), ivec3( 4,  2,  6), NoTri,             NoTri,            
    ivec3(10,  4,  9), ivec3(10,  6,  4), ivec3(11,  2,  3), NoTri,             NoTri,            
    ivec3( 0,  8,  2), ivec3( 2,  8, 11), ivec3( 4,  9, 10), ivec3( 4, 10,  6), NoTri,            
    ivec3( 3, 11,  2), ivec3( 0,  1,  6), ivec3( 0,  6,  4), ivec3( 6,  1, 10), NoTri,            
    ivec3( 6,  4,  1), ivec3( 6,  1, 10), ivec3( 4,  8,  1), ivec3( 2,  1, 11), ivec3( 8, 11,  1),
    ivec3( 9,  6,  4), ivec3( 9,  3,  6), ivec3( 9,  1,  3), ivec3(11,  6,  3), NoTri,            
    ivec3( 8, 11,  1), ivec3( 8,  1,  0), ivec3(11,  6,  1), ivec3( 9,  1,  4), ivec3( 6,  4,  1),
    ivec3( 3, 11,  6), ivec3( 3,  6,  0), ivec3( 0,  6,  4), NoTri,             NoTri,            
    ivec3( 6,  4,  8), ivec3(11,  6,  8), NoTri,             NoTri,             NoTri,              // 0x70
    ivec3( 7, 10,  6), ivec3( 7,  8, 10), ivec3( 8,  9, 10), NoTri,             NoTri,            
    ivec3( 0,  7,  3), ivec3( 0, 10,  7), ivec3( 0,  9, 10), ivec3( 6,  7, 10), NoTri,            
    ivec3(10,  6,  7), ivec3( 1, 10,  7), ivec3( 1,  7,  8), ivec3( 1,  8,  0), NoTri,            
    ivec3(10,  6,  7), ivec3(10,  7,  1), ivec3( 1,  7,  3), NoTri,             NoTri,            
    ivec3( 1,  2,  6), ivec3( 1,  6,  8), ivec3( 1,  8,  9), ivec3( 8,  6,  7), NoTri,            
    ivec3( 2,  6,  9), ivec3( 2,  9,  1), ivec3( 6,  7,  9), ivec3( 0,  9,  3), ivec3( 7,  3,  9),
    ivec3( 7,  8,  0), ivec3( 7,  0,  6), ivec3( 6,  0,  2), NoTri,             NoTri,            
    ivec3( 7,  3,  2), ivec3( 6,  7,  2), NoTri,             NoTri,             NoTri,            
    ivec3( 2,  3, 11), ivec3(10,  6,  8), ivec3(10,  8,  9), ivec3( 8,  6,  7), NoTri,            
    ivec3( 2,  0,  7), ivec3( 2,  7, 11), ivec3( 0,  9,  7), ivec3( 6,  7, 10), ivec3( 9, 10,  7),
    ivec3( 1,  8,  0), ivec3( 1,  7,  8), ivec3( 1, 10,  7), ivec3( 6,  7, 10), ivec3( 2,  3, 11),
    ivec3(11,  2,  1), ivec3(11,  1,  7), ivec3(10,  6,  1), ivec3( 6,  7,  1), NoTri,            
    ivec3( 8,  9,  6), ivec3( 8,  6,  7), ivec3( 9,  1,  6), ivec3(11,  6,  3), ivec3( 1,  3,  6),
    ivec3( 0,  9,  1), ivec3(11,  6,  7), NoTri,             NoTri,             NoTri,            
    ivec3( 7,  8,  0), ivec3( 7,  0,  6), ivec3( 3, 11,  0), ivec3(11,  6,  0), NoTri,            
    ivec3( 7, 11,  6), NoTri,             NoTri,             NoTri,             NoTri              // 0x80
    /*
    ivec3( 7,  6, 11), NoTri,             NoTri,             NoTri,             NoTri,            
    ivec3( 3,  0,  8), ivec3(11,  7,  6), NoTri,             NoTri,             NoTri,            
    ivec3( 0,  1,  9), ivec3(11,  7,  6), NoTri,             NoTri,             NoTri,            
    ivec3( 8,  1,  9), ivec3( 8,  3,  1), ivec3(11,  7,  6), NoTri,             NoTri,            
    ivec3(10,  1,  2), ivec3( 6, 11,  7), NoTri,             NoTri,             NoTri,            
    ivec3( 1,  2, 10), ivec3( 3,  0,  8), ivec3( 6, 11,  7), NoTri,             NoTri,            
    ivec3( 2,  9,  0), ivec3( 2, 10,  9), ivec3( 6, 11,  7), NoTri,             NoTri,            
    ivec3( 6, 11,  7), ivec3( 2, 10,  3), ivec3(10,  8,  3), ivec3(10,  9,  8), NoTri,            
    ivec3( 7,  2,  3), ivec3( 6,  2,  7), NoTri,             NoTri,             NoTri,            
    ivec3( 7,  0,  8), ivec3( 7,  6,  0), ivec3( 6,  2,  0), NoTri,             NoTri,            
    ivec3( 2,  7,  6), ivec3( 2,  3,  7), ivec3( 0,  1,  9), NoTri,             NoTri,            
    ivec3( 1,  6,  2), ivec3( 1,  8,  6), ivec3( 1,  9,  8), ivec3( 8,  7,  6), NoTri,            
    ivec3(10,  7,  6), ivec3(10,  1,  7), ivec3( 1,  3,  7), NoTri,             NoTri,            
    ivec3(10,  7,  6), ivec3( 1,  7, 10), ivec3( 1,  8,  7), ivec3( 1,  0,  8), NoTri,            
    ivec3( 0,  3,  7), ivec3( 0,  7, 10), ivec3( 0, 10,  9), ivec3( 6, 10,  7), NoTri,            
    ivec3( 7,  6, 10), ivec3( 7, 10,  8), ivec3( 8, 10,  9), NoTri,             NoTri,              // 0x90
    ivec3( 6,  8,  4), ivec3(11,  8,  6), NoTri,             NoTri,             NoTri,            
    ivec3( 3,  6, 11), ivec3( 3,  0,  6), ivec3( 0,  4,  6), NoTri,             NoTri,            
    ivec3( 8,  6, 11), ivec3( 8,  4,  6), ivec3( 9,  0,  1), NoTri,             NoTri,            
    ivec3( 9,  4,  6), ivec3( 9,  6,  3), ivec3( 9,  3,  1), ivec3(11,  3,  6), NoTri,            
    ivec3( 6,  8,  4), ivec3( 6, 11,  8), ivec3( 2, 10,  1), NoTri,             NoTri,            
    ivec3( 1,  2, 10), ivec3( 3,  0, 11), ivec3( 0,  6, 11), ivec3( 0,  4,  6), NoTri,            
    ivec3( 4, 11,  8), ivec3( 4,  6, 11), ivec3( 0,  2,  9), ivec3( 2, 10,  9), NoTri,            
    ivec3(10,  9,  3), ivec3(10,  3,  2), ivec3( 9,  4,  3), ivec3(11,  3,  6), ivec3( 4,  6,  3),
    ivec3( 8,  2,  3), ivec3( 8,  4,  2), ivec3( 4,  6,  2), NoTri,             NoTri,            
    ivec3( 0,  4,  2), ivec3( 4,  6,  2), NoTri,             NoTri,             NoTri,            
    ivec3( 1,  9,  0), ivec3( 2,  3,  4), ivec3( 2,  4,  6), ivec3( 4,  3,  8), NoTri,            
    ivec3( 1,  9,  4), ivec3( 1,  4,  2), ivec3( 2,  4,  6), NoTri,             NoTri,            
    ivec3( 8,  1,  3), ivec3( 8,  6,  1), ivec3( 8,  4,  6), ivec3( 6, 10,  1), NoTri,            
    ivec3(10,  1,  0), ivec3(10,  0,  6), ivec3( 6,  0,  4), NoTri,             NoTri,            
    ivec3( 4,  6,  3), ivec3( 4,  3,  8), ivec3( 6, 10,  3), ivec3( 0,  3,  9), ivec3(10,  9,  3),
    ivec3(10,  9,  4), ivec3( 6, 10,  4), NoTri,             NoTri,             NoTri,              // 0xA0
    ivec3( 4,  9,  5), ivec3( 7,  6, 11), NoTri,             NoTri,             NoTri,            
    ivec3( 0,  8,  3), ivec3( 4,  9,  5), ivec3(11,  7,  6), NoTri,             NoTri,            
    ivec3( 5,  0,  1), ivec3( 5,  4,  0), ivec3( 7,  6, 11), NoTri,             NoTri,            
    ivec3(11,  7,  6), ivec3( 8,  3,  4), ivec3( 3,  5,  4), ivec3( 3,  1,  5), NoTri,            
    ivec3( 9,  5,  4), ivec3(10,  1,  2), ivec3( 7,  6, 11), NoTri,             NoTri,            
    ivec3( 6, 11,  7), ivec3( 1,  2, 10), ivec3( 0,  8,  3), ivec3( 4,  9,  5), NoTri,            
    ivec3( 7,  6, 11), ivec3( 5,  4, 10), ivec3( 4,  2, 10), ivec3( 4,  0,  2), NoTri,            
    ivec3( 3,  4,  8), ivec3( 3,  5,  4), ivec3( 3,  2,  5), ivec3(10,  5,  2), ivec3(11,  7,  6),
    ivec3( 7,  2,  3), ivec3( 7,  6,  2), ivec3( 5,  4,  9), NoTri,             NoTri,            
    ivec3( 9,  5,  4), ivec3( 0,  8,  6), ivec3( 0,  6,  2), ivec3( 6,  8,  7), NoTri,            
    ivec3( 3,  6,  2), ivec3( 3,  7,  6), ivec3( 1,  5,  0), ivec3( 5,  4,  0), NoTri,            
    ivec3( 6,  2,  8), ivec3( 6,  8,  7), ivec3( 2,  1,  8), ivec3( 4,  8,  5), ivec3( 1,  5,  8),
    ivec3( 9,  5,  4), ivec3(10,  1,  6), ivec3( 1,  7,  6), ivec3( 1,  3,  7), NoTri,            
    ivec3( 1,  6, 10), ivec3( 1,  7,  6), ivec3( 1,  0,  7), ivec3( 8,  7,  0), ivec3( 9,  5,  4),
    ivec3( 4,  0, 10), ivec3( 4, 10,  5), ivec3( 0,  3, 10), ivec3( 6, 10,  7), ivec3( 3,  7, 10),
    ivec3( 7,  6, 10), ivec3( 7, 10,  8), ivec3( 5,  4, 10), ivec3( 4,  8, 10), NoTri,              // 0xB0
    ivec3( 6,  9,  5), ivec3( 6, 11,  9), ivec3(11,  8,  9), NoTri,             NoTri,            
    ivec3( 3,  6, 11), ivec3( 0,  6,  3), ivec3( 0,  5,  6), ivec3( 0,  9,  5), NoTri,            
    ivec3( 0, 11,  8), ivec3( 0,  5, 11), ivec3( 0,  1,  5), ivec3( 5,  6, 11), NoTri,            
    ivec3( 6, 11,  3), ivec3( 6,  3,  5), ivec3( 5,  3,  1), NoTri,             NoTri,            
    ivec3( 1,  2, 10), ivec3( 9,  5, 11), ivec3( 9, 11,  8), ivec3(11,  5,  6), NoTri,            
    ivec3( 0, 11,  3), ivec3( 0,  6, 11), ivec3( 0,  9,  6), ivec3( 5,  6,  9), ivec3( 1,  2, 10),
    ivec3(11,  8,  5), ivec3(11,  5,  6), ivec3( 8,  0,  5), ivec3(10,  5,  2), ivec3( 0,  2,  5),
    ivec3( 6, 11,  3), ivec3( 6,  3,  5), ivec3( 2, 10,  3), ivec3(10,  5,  3), NoTri,            
    ivec3( 5,  8,  9), ivec3( 5,  2,  8), ivec3( 5,  6,  2), ivec3( 3,  8,  2), NoTri,            
    ivec3( 9,  5,  6), ivec3( 9,  6,  0), ivec3( 0,  6,  2), NoTri,             NoTri,            
    ivec3( 1,  5,  8), ivec3( 1,  8,  0), ivec3( 5,  6,  8), ivec3( 3,  8,  2), ivec3( 6,  2,  8),
    ivec3( 1,  5,  6), ivec3( 2,  1,  6), NoTri,             NoTri,             NoTri,            
    ivec3( 1,  3,  6), ivec3( 1,  6, 10), ivec3( 3,  8,  6), ivec3( 5,  6,  9), ivec3( 8,  9,  6),
    ivec3(10,  1,  0), ivec3(10,  0,  6), ivec3( 9,  5,  0), ivec3( 5,  6,  0), NoTri,            
    ivec3( 0,  3,  8), ivec3( 5,  6, 10), NoTri,             NoTri,             NoTri,            
    ivec3(10,  5,  6), NoTri,             NoTri,             NoTri,             NoTri,              // 0xC0
    ivec3(11,  5, 10), ivec3( 7,  5, 11), NoTri,             NoTri,             NoTri,            
    ivec3(11,  5, 10), ivec3(11,  7,  5), ivec3( 8,  3,  0), NoTri,             NoTri,            
    ivec3( 5, 11,  7), ivec3( 5, 10, 11), ivec3( 1,  9,  0), NoTri,             NoTri,            
    ivec3(10,  7,  5), ivec3(10, 11,  7), ivec3( 9,  8,  1), ivec3( 8,  3,  1), NoTri,            
    ivec3(11,  1,  2), ivec3(11,  7,  1), ivec3( 7,  5,  1), NoTri,             NoTri,            
    ivec3( 0,  8,  3), ivec3( 1,  2,  7), ivec3( 1,  7,  5), ivec3( 7,  2, 11), NoTri,            
    ivec3( 9,  7,  5), ivec3( 9,  2,  7), ivec3( 9,  0,  2), ivec3( 2, 11,  7), NoTri,            
    ivec3( 7,  5,  2), ivec3( 7,  2, 11), ivec3( 5,  9,  2), ivec3( 3,  2,  8), ivec3( 9,  8,  2),
    ivec3( 2,  5, 10), ivec3( 2,  3,  5), ivec3( 3,  7,  5), NoTri,             NoTri,            
    ivec3( 8,  2,  0), ivec3( 8,  5,  2), ivec3( 8,  7,  5), ivec3(10,  2,  5), NoTri,            
    ivec3( 9,  0,  1), ivec3( 5, 10,  3), ivec3( 5,  3,  7), ivec3( 3, 10,  2), NoTri,            
    ivec3( 9,  8,  2), ivec3( 9,  2,  1), ivec3( 8,  7,  2), ivec3(10,  2,  5), ivec3( 7,  5,  2),
    ivec3( 1,  3,  5), ivec3( 3,  7,  5), NoTri,             NoTri,             NoTri,            
    ivec3( 0,  8,  7), ivec3( 0,  7,  1), ivec3( 1,  7,  5), NoTri,             NoTri,            
    ivec3( 9,  0,  3), ivec3( 9,  3,  5), ivec3( 5,  3,  7), NoTri,             NoTri,            
    ivec3( 9,  8,  7), ivec3( 5,  9,  7), NoTri,             NoTri,             NoTri,              // 0xD0
    ivec3( 5,  8,  4), ivec3( 5, 10,  8), ivec3(10, 11,  8), NoTri,             NoTri,            
    ivec3( 5,  0,  4), ivec3( 5, 11,  0), ivec3( 5, 10, 11), ivec3(11,  3,  0), NoTri,            
    ivec3( 0,  1,  9), ivec3( 8,  4, 10), ivec3( 8, 10, 11), ivec3(10,  4,  5), NoTri,            
    ivec3(10, 11,  4), ivec3(10,  4,  5), ivec3(11,  3,  4), ivec3( 9,  4,  1), ivec3( 3,  1,  4),
    ivec3( 2,  5,  1), ivec3( 2,  8,  5), ivec3( 2, 11,  8), ivec3( 4,  5,  8), NoTri,            
    ivec3( 0,  4, 11), ivec3( 0, 11,  3), ivec3( 4,  5, 11), ivec3( 2, 11,  1), ivec3( 5,  1, 11),
    ivec3( 0,  2,  5), ivec3( 0,  5,  9), ivec3( 2, 11,  5), ivec3( 4,  5,  8), ivec3(11,  8,  5),
    ivec3( 9,  4,  5), ivec3( 2, 11,  3), NoTri,             NoTri,             NoTri,            
    ivec3( 2,  5, 10), ivec3( 3,  5,  2), ivec3( 3,  4,  5), ivec3( 3,  8,  4), NoTri,            
    ivec3( 5, 10,  2), ivec3( 5,  2,  4), ivec3( 4,  2,  0), NoTri,             NoTri,            
    ivec3( 3, 10,  2), ivec3( 3,  5, 10), ivec3( 3,  8,  5), ivec3( 4,  5,  8), ivec3( 0,  1,  9),
    ivec3( 5, 10,  2), ivec3( 5,  2,  4), ivec3( 1,  9,  2), ivec3( 9,  4,  2), NoTri,            
    ivec3( 8,  4,  5), ivec3( 8,  5,  3), ivec3( 3,  5,  1), NoTri,             NoTri,            
    ivec3( 0,  4,  5), ivec3( 1,  0,  5), NoTri,             NoTri,             NoTri,            
    ivec3( 8,  4,  5), ivec3( 8,  5,  3), ivec3( 9,  0,  5), ivec3( 0,  3,  5), NoTri,            
    ivec3( 9,  4,  5), NoTri,             NoTri,             NoTri,             NoTri,              // 0xE0
    ivec3( 4, 11,  7), ivec3( 4,  9, 11), ivec3( 9, 10, 11), NoTri,             NoTri,            
    ivec3( 0,  8,  3), ivec3( 4,  9,  7), ivec3( 9, 11,  7), ivec3( 9, 10, 11), NoTri,            
    ivec3( 1, 10, 11), ivec3( 1, 11,  4), ivec3( 1,  4,  0), ivec3( 7,  4, 11), NoTri,            
    ivec3( 3,  1,  4), ivec3( 3,  4,  8), ivec3( 1, 10,  4), ivec3( 7,  4, 11), ivec3(10, 11,  4),
    ivec3( 4, 11,  7), ivec3( 9, 11,  4), ivec3( 9,  2, 11), ivec3( 9,  1,  2), NoTri,            
    ivec3( 9,  7,  4), ivec3( 9, 11,  7), ivec3( 9,  1, 11), ivec3( 2, 11,  1), ivec3( 0,  8,  3),
    ivec3(11,  7,  4), ivec3(11,  4,  2), ivec3( 2,  4,  0), NoTri,             NoTri,            
    ivec3(11,  7,  4), ivec3(11,  4,  2), ivec3( 8,  3,  4), ivec3( 3,  2,  4), NoTri,            
    ivec3( 2,  9, 10), ivec3( 2,  7,  9), ivec3( 2,  3,  7), ivec3( 7,  4,  9), NoTri,            
    ivec3( 9, 10,  7), ivec3( 9,  7,  4), ivec3(10,  2,  7), ivec3( 8,  7,  0), ivec3( 2,  0,  7),
    ivec3( 3,  7, 10), ivec3( 3, 10,  2), ivec3( 7,  4, 10), ivec3( 1, 10,  0), ivec3( 4,  0, 10),
    ivec3( 1, 10,  2), ivec3( 8,  7,  4), NoTri,             NoTri,             NoTri,            
    ivec3( 4,  9,  1), ivec3( 4,  1,  7), ivec3( 7,  1,  3), NoTri,             NoTri,            
    ivec3( 4,  9,  1), ivec3( 4,  1,  7), ivec3( 0,  8,  1), ivec3( 8,  7,  1), NoTri,            
    ivec3( 4,  0,  3), ivec3( 7,  4,  3), NoTri,             NoTri,             NoTri,            
    ivec3( 4,  8,  7), NoTri,             NoTri,             NoTri,             NoTri,              // 0xF0
    ivec3( 9, 10,  8), ivec3(10, 11,  8), NoTri,             NoTri,             NoTri,            
    ivec3( 3,  0,  9), ivec3( 3,  9, 11), ivec3(11,  9, 10), NoTri,             NoTri,            
    ivec3( 0,  1, 10), ivec3( 0, 10,  8), ivec3( 8, 10, 11), NoTri,             NoTri,            
    ivec3( 3,  1, 10), ivec3(11,  3, 10), NoTri,             NoTri,             NoTri,            
    ivec3( 1,  2, 11), ivec3( 1, 11,  9), ivec3( 9, 11,  8), NoTri,             NoTri,            
    ivec3( 3,  0,  9), ivec3( 3,  9, 11), ivec3( 1,  2,  9), ivec3( 2, 11,  9), NoTri,            
    ivec3( 0,  2, 11), ivec3( 8,  0, 11), NoTri,             NoTri,             NoTri,            
    ivec3( 3,  2, 11), NoTri,             NoTri,             NoTri,             NoTri,            
    ivec3( 2,  3,  8), ivec3( 2,  8, 10), ivec3(10,  8,  9), NoTri,             NoTri,            
    ivec3( 9, 10,  2), ivec3( 0,  9,  2), NoTri,             NoTri,             NoTri,            
    ivec3( 2,  3,  8), ivec3( 2,  8, 10), ivec3( 0,  1,  8), ivec3( 1, 10,  8), NoTri,            
    ivec3( 1, 10,  2), NoTri,             NoTri,             NoTri,             NoTri,            
    ivec3( 1,  3,  8), ivec3( 9,  1,  8), NoTri,             NoTri,             NoTri,            
    ivec3( 0,  9,  1), NoTri,             NoTri,             NoTri,             NoTri,            
    ivec3( 0,  3,  8), NoTri,             NoTri,             NoTri,             NoTri,            
    NoTri,             NoTri,             NoTri,             NoTri,             NoTri*/
);

float NodeAt(uvec3 aIndex)
{
    const uint WidthSlice = Size.x;
    const uint DepthSlice = WidthSlice * Size.y;

    return float(NodeData[aIndex.z 
                    + aIndex.y * WidthSlice
                    + aIndex.x * DepthSlice]);
}

vec3 Lerp(vec3 a, vec3 b, float v)
{
    return a + (b - a) * v;
}

float InverseLerp(float a, float b, float v)
{
    return (v - a) / (b - a);
}

vec4 PointOnEdge(int aFirst, int aSecond)
{
    uvec3 at = gl_GlobalInvocationID.xyz;

    float weight = InverseLerp(
                        NodeAt(at + Corners[aFirst]), 
                        NodeAt(at + Corners[aSecond]), 
                        Surface);

    return vec4(vec3(at) + Lerp(
                        vec3(Corners[aFirst]), 
                        vec3(Corners[aSecond]), 
                        weight), 0);
}


void main() {
    
    //TriCount = 0;
    //memoryBarrierBuffer();
    //barrier();
    uvec3 at = gl_GlobalInvocationID.xyz;

    if(at.x >= Size.x
    || at.y >= Size.y
    || at.z >= Size.z)
    {
        return;
    }
    
    uint index = 0;
    for (int i = 0; i < 7; i++)
    {
        if (NodeAt(at + Corners[i]) >= Surface)
        {
            index += 1 << i;
        }
    }

    bool invert = NodeAt(at + Corners[7]) >= Surface;

    if(invert)
    {
        index = (~index) & 0x7F;
    }

    
    const vec4[12] edges = vec4[12]
    (
        PointOnEdge(0, 1) * Scale,
        PointOnEdge(1, 2) * Scale,
        PointOnEdge(2, 3) * Scale,
        PointOnEdge(3, 0) * Scale,
        PointOnEdge(4, 5) * Scale,
        PointOnEdge(5, 6) * Scale,
        PointOnEdge(6, 7) * Scale,
        PointOnEdge(7, 4) * Scale,
        PointOnEdge(0, 4) * Scale,
        PointOnEdge(1, 5) * Scale,
        PointOnEdge(2, 6) * Scale,
        PointOnEdge(3, 7) * Scale
    );
    
    uint base = index * MaxTrisPer; 

    for (int i = 0; i < MaxTrisPer; i++)
    {
        ivec3 tri = TriTable[base + i];
    
        if (tri == NoTri)
            return;
    
        uint index = atomicAdd(TriCount, 1);
    
        if (index >= MaxTris)
            return;
    
        if (invert)
        {
            Tris[index].a = edges[tri.x];
            Tris[index].b = edges[tri.z];
            Tris[index].c = edges[tri.y];
        }
        else
        {
            Tris[index].a = edges[tri.x];
            Tris[index].b = edges[tri.y];
            Tris[index].c = edges[tri.z];
        }
    }
}