#include "include/lua.hpp"
#include<iostream>
#include<string>

bool enable_infinitive_loop = false;
lua_State* Lua = luaL_newstate();

void PrintIfError(int res)
{
    if (res != 0)
        printf("%s\n", lua_tostring(Lua, -1));
}

bool TryCommand(const char* cmd)
{
    int res = luaL_dostring(Lua, cmd);

    PrintIfError(res);
    return res;
}

std::string readln()
{
    std::string input;
    std::getline(std::cin, input);

    return input;
}

enum class ARG_STATE
{
    DEFAULT,
    RUN,
    FILE,
};

ARG_STATE ToEnum(const char* state)
{
    std::string str(state);

    if (str == "run")
        return ARG_STATE::RUN;
    if (str == "file")
        return ARG_STATE::FILE;

    return ARG_STATE::DEFAULT;
}


int main(int argc, const char** argv)
{
    luaL_openlibs(Lua);
    if (argc > 1)
        switch (ToEnum(argv[1]))
        {
        case ARG_STATE::RUN:
        {
            std::string compared;
            for (int i = 2; i < argc; i++)
            {
                compared += argv[i];
            }

            return TryCommand(compared.c_str());
        }
        case ARG_STATE::FILE:
        {
            int res = luaL_dofile(Lua, argv[2]);

            PrintIfError(res);
            return res;
        }
        default:
            printf("Unexpected arg \"%s\"\n", argv[1]);
            return 1;
        }

    while (true)
    {
        std::string cmd = readln();

        if (cmd == "exit")
            return 0;

        TryCommand(cmd.c_str());
    }
}
