#include <iostream>

int add(int a, int b)
{
    return a + b;
}

int main()
{
    int result = add(3, 4);
    std::cout << result << std::endl;
    return 0;
}