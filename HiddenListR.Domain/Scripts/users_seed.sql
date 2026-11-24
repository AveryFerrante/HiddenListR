INSERT INTO Users (Id, FirstName, LastName, PhoneNumber, Role)
SELECT 1, 'Admin', 'User', '1111111111', 1
WHERE NOT EXISTS (SELECT 1 FROM Users WHERE PhoneNumber = '1111111111');

INSERT INTO Users (Id, FirstName, LastName, PhoneNumber, Role)
SELECT 2, 'Test', 'User', '0000000000', 0
WHERE NOT EXISTS (SELECT 1 FROM Users WHERE PhoneNumber = '0000000000');
