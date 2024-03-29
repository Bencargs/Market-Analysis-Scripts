# Market-Analysis

Various experiments using share market data


**ASX300 during the 2007 Financial Crisis converted to audio**
![test.wav](https://github.com/Bencargs/Market-Analysis/blob/master/test.wav)


**Probability Distrobution Function of ASX 300**
![ASX300 Price Histogram (gradient)](https://i.imgur.com/mRwMxwl.png)


**Probability Distrobution Function of ASX 300 (Colour Scale)**
![ASX300 Price Histogram (colour scale)](https://i.imgur.com/8QDsMJx.png)

## Details
Data is generated by taking the average Mean and Histograms of price movements for each day over a
reference ‘window’ using the 15 years of the ASX 300.

Each day of the dataset was categorised in their relevant window, and price and
frequencies calculated.
The values for the daily histogram for each reference window can than be charted, with price spread
on the Y axis, and day of the year on the X, and frequency of price point represented by the intensity
of the colour. As below:
![Reference Windows Probability Distrobution Frequencies](https://i.imgur.com/SJ4GpyL.png)

The trends are then combined into a single image. Frequencies are intensified die to constructive interference.

**Random Walk on ASX 300**
![Random Walk on ASX 300](https://i.imgur.com/FbLGVO5.png)

Using the above PDF, a random walk simulator was constructed to reveal broard statistical trends and averages.

## Simulation

Further work involved simulating optimal buy stratergies against the dataset.
Identified two methods (Moving Average and Beta) achieving excess returns over daily market average.

Statistical significance tested by calculating daily alpha over a constant daily buy stratergy.
Randomised start times and monte carlo averaging.

![Daily Price Delta by Buy Stratergy](https://i.imgur.com/K8Jv3V9.png)


![Buy Simulator](https://github.com/Bencargs/Market-Analysis/blob/master/BuySimulator.linq)

**Optimal Buy Time Pattern Recognition**
Ongoing work around using image processing techniques to support market price pattern recognition.
To involve converting market data into image signals,
building average or eigenface image of pre market conditions from a training set, 
and having system identify matching patterns.

![Stock Market Pattern Recognition](https://i.imgur.com/mkGrRgN.gif)
