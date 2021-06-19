function main()
	local str="aalalal";
	str:gsub("aa", "a");
	local k=0;
	if (str=="lalal") then
		for i=0, 10 do 
			k=k+#str;
			break;
		end
	end
end